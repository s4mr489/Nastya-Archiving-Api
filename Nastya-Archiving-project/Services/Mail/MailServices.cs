using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Nastya_Archiving_project.Data;
using Nastya_Archiving_project.Models;
using Nastya_Archiving_project.Models.DTOs;
using Nastya_Archiving_project.Models.DTOs.mail;
using Nastya_Archiving_project.Seginal;
using Nastya_Archiving_project.Services.ArchivingSettings;
using Nastya_Archiving_project.Services.encrpytion;
using Nastya_Archiving_project.Services.infrastructure;
using Nastya_Archiving_project.Services.SystemInfo;
using System.Collections.Concurrent;

namespace Nastya_Archiving_project.Services.Mail
{
    public class MailServices : IMailServices
    {
        private readonly AppDbContext _context;
        private readonly IArchivingSettingsServicers _archivingSettingsServicers;
        private readonly IInfrastructureServices _infrastructureServices;
        private readonly ISystemInfoServices _systemInfoServices;
        private readonly IEncryptionServices _encryptionServices;
        private readonly IHubContext<MailNotificationHub> _hubContext;
        private readonly IMemoryCache _cache;
        private static readonly TimeSpan DocTypeCacheDuration = TimeSpan.FromHours(6);
        private static readonly ConcurrentDictionary<string, int> _userUnreadCounts = new();

        public MailServices(
            AppDbContext context,
            IArchivingSettingsServicers archivingSettingsServicers,
            IInfrastructureServices infrastructureServices,
            ISystemInfoServices systemInfoServices,
            IEncryptionServices encryptionServices,
            IHubContext<MailNotificationHub> hubContext,
            IMemoryCache cache)
        {
            _context = context;
            _archivingSettingsServicers = archivingSettingsServicers;
            _infrastructureServices = infrastructureServices;
            _systemInfoServices = systemInfoServices;
            _encryptionServices = encryptionServices;
            _hubContext = hubContext;
            _cache = cache;
        }

        /// <summary>
        /// Sends a mail to one or more recipients with optional document reference
        /// </summary>
        /// <param name="req">Mail details including recipients and document reference</param>
        /// <returns>Response with mail details or error information</returns>
        public async Task<BaseResponseDTOs> SendMail(MailViewForm req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.to))
                return new BaseResponseDTOs(null, 400, "Invalid request data: No recipients specified");

            try
            {
                // Get sender's name - we only need to do this once
                var (realName, error) = await _systemInfoServices.GetRealName();
                if (!string.IsNullOrEmpty(error))
                    return new BaseResponseDTOs(null, 400, error);

                // Process recipient list efficiently - use array directly for better performance
                string[] recipientArray = req.to.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

                if (recipientArray.Length == 0)
                    return new BaseResponseDTOs(null, 400, "No valid recipients specified");

                // Use HashSet for faster lookups and automatic deduplication
                var recipientSet = new HashSet<string>(
                    recipientArray.Select(r => r.Trim()).Where(r => !string.IsNullOrWhiteSpace(r)),
                    StringComparer.OrdinalIgnoreCase
                );

                if (recipientSet.Count == 0)
                    return new BaseResponseDTOs(null, 400, "No valid recipients specified");

                // Pre-encrypt all recipients in one batch - saves encryption time
                var encryptedRecipients = new Dictionary<string, string>(recipientSet.Count);
                foreach (var recipient in recipientSet)
                {
                    encryptedRecipients[recipient] = _encryptionServices.EncryptString256Bit(recipient);
                }

                // Validate recipients in a single database query instead of multiple queries
                var existingUsers = await _context.Users
                    .AsNoTracking()
                    .Where(u => encryptedRecipients.Values.Contains(u.Realname))
                    .Select(u => u.Realname)
                    .ToHashSetAsync();

                // Determine invalid recipients efficiently
                var validRecipients = new List<string>(recipientSet.Count);
                var invalidRecipients = new List<string>();

                foreach (var kvp in encryptedRecipients)
                {
                    if (existingUsers.Contains(kvp.Value))
                        validRecipients.Add(kvp.Key);
                    else
                        invalidRecipients.Add(kvp.Key);
                }

                if (invalidRecipients.Count > 0)
                    return new BaseResponseDTOs(null, 400, $"Recipients not found: {string.Join(", ", invalidRecipients)}");

                // Get document information with a single optimized query
                object documentInfo = null;
                if (!string.IsNullOrEmpty(req.ReferenceNo))
                {
                    // Use a single query with join to get document and doc type in one trip to database
                    var documentWithType = await (from doc in _context.ArcivingDocs
                                                  join type in _context.ArcivDocDscrps
                                                  on doc.DocType equals type.Id into typeJoin
                                                  from type in typeJoin.DefaultIfEmpty()
                                                  where doc.RefrenceNo == req.ReferenceNo
                                                  select new
                                                  {
                                                      doc.Id,
                                                      doc.RefrenceNo,
                                                      doc.DocNo,
                                                      doc.DocDate,
                                                      doc.DocType,
                                                      DocTypeName = type.Dscrp,
                                                      doc.Subject,
                                                      doc.ImgUrl
                                                  })
                                                .AsNoTracking()
                                                .FirstOrDefaultAsync();

                    documentInfo = documentWithType;
                }

                // Pre-allocate collections with exact capacity needed
                var now = DateTime.Now;
                var createdMails = new List<TFileTransferring>(validRecipients.Count);
                var mailTasks = new List<Task>(validRecipients.Count * 2); // For notification tasks

                // Create all mail entities at once
                foreach (var recipient in validRecipients)
                {
                    var mail = new TFileTransferring
                    {
                        RefrenceNo = req.ReferenceNo,
                        From = realName,
                        To = recipient,
                        Notes = req.Notes,
                        SendDate = now,
                        Readed = 0
                    };

                    createdMails.Add(mail);
                    // Update unread count for this recipient
                    _userUnreadCounts.AddOrUpdate(recipient, 1, (_, count) => count + 1);
                }

                // Add all entities in a single batch and save once
                _context.TFileTransferrings.AddRange(createdMails);
                await _context.SaveChangesAsync();

                // Prepare sender name once for all notifications
                var senderName = TryDecryptName(realName);

                // Queue up notifications for each recipient to be sent in parallel
                foreach (var mail in createdMails)
                {
                    // Send real-time notification with document info
                    mailTasks.Add(_hubContext.Clients.User(mail.To).SendAsync("ReceiveNewMail", new
                    {
                        Id = mail.Id,
                        From = realName,
                        SenderName = senderName,
                        To = mail.To,
                        ReferenceNo = req.ReferenceNo,
                        Notes = req.Notes,
                        SendDate = mail.SendDate,
                        IsRead = false,
                        Document = documentInfo
                    }));

                    // Also broadcast updated count
                    mailTasks.Add(_hubContext.Clients.User(mail.To).SendAsync("UpdateMailCount", new
                    {
                        UnreadCount = _userUnreadCounts.GetValueOrDefault(mail.To, 0)
                    }));
                }

                // Wait for all notifications to complete at once
                if (mailTasks.Count > 0)
                {
                    await Task.WhenAll(mailTasks);
                }

                // Return response with all mail information
                return new BaseResponseDTOs(new
                {
                    MailsSent = createdMails.Count,
                    Recipients = validRecipients,
                    ReferenceNo = req.ReferenceNo,
                    SentDate = now
                }, 200, $"Mail sent successfully to {createdMails.Count} recipient(s)");
            }
            catch (Exception ex)
            {
                return new BaseResponseDTOs(null, 500, $"Failed to send mail: {ex.Message}");
            }
        }

        public async Task<BaseResponseDTOs> GetAllMails()
        {
            var response = await GetMailsInternal(includeRead: true);

            // Send real-time update with the latest data
            try
            {
                var (realName, _) = await _systemInfoServices.GetRealName();
                if (!string.IsNullOrEmpty(realName))
                {
                    await _hubContext.Clients.User(realName).SendAsync("MailListUpdated", response.Data);
                }
            }
            catch
            {
                // Don't let SignalR issues affect the response
            }

            return response;
        }

        public async Task<BaseResponseDTOs> GetUnreadMails()
        {
            var response = await GetMailsInternal(includeRead: false);

            // Update the in-memory unread count
            try
            {
                var (realName, _) = await _systemInfoServices.GetRealName();
                if (!string.IsNullOrEmpty(realName))
                {
                    var responseData = response.Data as dynamic;
                    int unreadCount = responseData?.UnreadCount ?? 0;
                    _userUnreadCounts.AddOrUpdate(realName, unreadCount, (_, __) => unreadCount);

                    // Real-time notification of unread count
                    await _hubContext.Clients.User(realName).SendAsync("UnreadMailCount", unreadCount);
                }
            }
            catch
            {
                // Don't let SignalR issues affect the response
            }

            return response;
        }

        public async Task<BaseResponseDTOs> MarkMailAsRead(int mailId)
        {
            try
            {
                var (realName, error) = await _systemInfoServices.GetRealName();
                if (!string.IsNullOrEmpty(error))
                    return new BaseResponseDTOs(null, 404, "User identification not available");

                // Find the mail
                var mail = await _context.TFileTransferrings
                    .Where(m => m.Id == mailId && m.To == realName && m.Readed == 0)
                    .FirstOrDefaultAsync();

                if (mail == null)
                    return new BaseResponseDTOs(null, 404, "Mail not found or already read");

                // Update read status
                mail.Readed = 1;
                await _context.SaveChangesAsync();

                // Update cached unread count
                _userUnreadCounts.AddOrUpdate(realName, 0, (key, count) => Math.Max(0, count - 1));

                // Notify real-time of mail read status change
                await _hubContext.Clients.User(realName).SendAsync("MailRead", new
                {
                    Id = mailId,
                    UnreadCount = _userUnreadCounts[realName]
                });

                return new BaseResponseDTOs(new { Id = mailId, IsRead = true }, 200);
            }
            catch (Exception ex)
            {
                return new BaseResponseDTOs(null, 500, $"Error updating mail: {ex.Message}");
            }
        }

        public async Task<BaseResponseDTOs> GetMailCount()
        {
            try
            {
                var (realName, error) = await _systemInfoServices.GetRealName();
                if (!string.IsNullOrEmpty(error))
                    return new BaseResponseDTOs(null, 404, "User identification not available");

                // Query for counts
                var totalCount = await _context.TFileTransferrings
                    .AsNoTracking()
                    .CountAsync(m => m.To == realName);

                var unreadCount = await _context.TFileTransferrings
                    .AsNoTracking()
                    .CountAsync(m => m.To == realName && m.Readed == 0);

                // Update in-memory count
                _userUnreadCounts[realName] = unreadCount;

                // Send real-time notification
                await _hubContext.Clients.User(realName).SendAsync("UnreadMailCount", unreadCount);

                return new BaseResponseDTOs(new
                {
                    TotalCount = totalCount,
                    UnreadCount = unreadCount
                }, 200);
            }
            catch (Exception ex)
            {
                return new BaseResponseDTOs(null, 500, $"Error getting mail counts: {ex.Message}");
            }
        }

        // Consolidated mail retrieval logic
        private async Task<BaseResponseDTOs> GetMailsInternal(bool includeRead)
        {
            try
            {
                var (realName, error) = await _systemInfoServices.GetRealName();
                if (!string.IsNullOrEmpty(error))
                    return new BaseResponseDTOs(null, 404, "User identification not available");

                // Build query based on read status
                var query = _context.TFileTransferrings
                    .AsNoTracking()
                    .Where(u => u.To == realName);

                if (!includeRead)
                    query = query.Where(m => m.Readed == 0);

                // Get mails with efficient projection
                var mails = await query
                    .OrderByDescending(u => u.SendDate)
                    .Select(m => new
                    {
                        m.Id,
                        m.RefrenceNo,
                        m.From,
                        m.To,
                        m.Notes,
                        m.SendDate,
                        m.Readed
                    })
                    .ToListAsync();

                if (!mails.Any())
                {
                    string message = includeRead ? "No mails found" : "No unread mails found";

                    // Update unread count in memory
                    _userUnreadCounts[realName] = 0;

                    // Real-time notification of empty state
                    await _hubContext.Clients.User(realName).SendAsync("MailListStatus", new
                    {
                        IsEmpty = true,
                        Message = message,
                        UnreadCount = 0
                    });

                    return new BaseResponseDTOs(new { Items = Array.Empty<object>(), Count = 0 }, 200, message);
                }

                // Process distinct reference numbers efficiently
                var refNosSet = mails
                    .Where(m => !string.IsNullOrEmpty(m.RefrenceNo))
                    .Select(m => m.RefrenceNo)
                    .Distinct()
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                // Get documents with safety against duplicate keys
                var documentsMap = await GetDocumentsWithSafetyAsync(refNosSet);

                // Get document types from cache or database
                var docTypeDict = await GetDocumentTypesAsync(documentsMap);

                // Process results efficiently
                var result = new List<object>(mails.Count);
                var unreadIds = new List<int>();

                foreach (var mail in mails)
                {
                    if (mail.Readed == 0)
                        unreadIds.Add(mail.Id);

                    // Get document info if available
                    object doc = null;

                    if (!string.IsNullOrEmpty(mail.RefrenceNo) &&
                        documentsMap.TryGetValue(mail.RefrenceNo, out var docObj))
                    {
                        var docDyn = (dynamic)docObj;
                        string docTypeName = null;

                        if (docTypeDict.TryGetValue((int)docDyn.DocType, out var typeName))
                            docTypeName = typeName;

                        doc = new
                        {
                            docDyn.Id,
                            docDyn.RefrenceNo,
                            docDyn.DocNo,
                            docDyn.DocDate,
                            docDyn.DocType,
                            DocTypeName = docTypeName,
                            docDyn.Subject,
                            docDyn.ImgUrl
                        };
                    }

                    result.Add(new
                    {
                        Mail = new
                        {
                            mail.Id,
                            mail.RefrenceNo,
                            mail.From,
                            SenderName = TryDecryptName(mail.From),
                            mail.To,
                            mail.Notes,
                            mail.SendDate,
                            IsRead = mail.Readed == 1
                        },
                        Document = doc
                    });
                }

                // Update read status if needed (for GetAllMails only)
                if (includeRead && unreadIds.Any())
                {
                    await MarkMailsAsReadAsync(unreadIds);

                    // Update unread count in memory
                    _userUnreadCounts[realName] = 0;

                    // Real-time notification of mail status change
                    await _hubContext.Clients.User(realName).SendAsync("AllMailsRead");
                }
                else if (!includeRead && unreadIds.Any())
                {
                    // Update unread count in memory for unread mails view
                    _userUnreadCounts[realName] = unreadIds.Count;
                }

                var response = new
                {
                    Items = result,
                    Count = mails.Count,
                    UnreadCount = unreadIds.Count,
                    Timestamp = DateTime.UtcNow
                };

                // Send real-time update
                await _hubContext.Clients.User(realName).SendAsync("MailDataUpdate", response);

                return new BaseResponseDTOs(response, 200);
            }
            catch (DbUpdateException dbEx)
            {
                return new BaseResponseDTOs(null, 500, $"Database error: {dbEx.InnerException?.Message ?? dbEx.Message}");
            }
            catch (Exception ex)
            {
                return new BaseResponseDTOs(null, 500, $"Error retrieving mails: {ex.Message}");
            }
        }

        // Helper to get documents with safety against duplicate reference numbers
        private async Task<Dictionary<string, object>> GetDocumentsWithSafetyAsync(HashSet<string> refNos)
        {
            if (!refNos.Any())
                return new Dictionary<string, object>();

            var documents = await _context.ArcivingDocs
                .AsNoTracking()
                .Where(d => refNos.Contains(d.RefrenceNo))
                .Select(d => new
                {
                    d.RefrenceNo,
                    d.Id,
                    d.DocNo,
                    d.DocDate,
                    d.DocType,
                    d.Subject,
                    d.ImgUrl
                })
                .ToListAsync();

            // Handle potential duplicate keys safely
            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var doc in documents)
            {
                if (!result.ContainsKey(doc.RefrenceNo))
                    result[doc.RefrenceNo] = doc;
            }

            return result;
        }

        // Get document types with caching for performance
        private async Task<Dictionary<int, string>> GetDocumentTypesAsync(Dictionary<string, object> documentsMap)
        {
            if (!documentsMap.Any())
                return new Dictionary<int, string>();

            // Extract unique document type IDs
            var docTypeIds = new HashSet<int>();
            foreach (var doc in documentsMap.Values)
            {
                var docType = (int)((dynamic)doc).DocType;
                docTypeIds.Add(docType);
            }

            // Check cache first
            var cacheKey = "DocTypes";
            if (!_cache.TryGetValue(cacheKey, out Dictionary<int, string> docTypeDict))
            {
                // Cache miss - get from database
                docTypeDict = await _context.ArcivDocDscrps
                    .AsNoTracking()
                    .ToDictionaryAsync(dt => dt.Id, dt => dt.Dscrp);

                // Store in cache
                _cache.Set(cacheKey, docTypeDict, DocTypeCacheDuration);
            }

            return docTypeDict;
        }

        // Efficiently mark multiple mails as read in one database call
        private async Task MarkMailsAsReadAsync(List<int> mailIds)
        {
            if (!mailIds.Any())
                return;

            // Use ExecuteUpdateAsync for better performance
            await _context.TFileTransferrings
                .Where(m => mailIds.Contains(m.Id))
                .ExecuteUpdateAsync(s => s.SetProperty(m => m.Readed, 1));

            // Notify via SignalR about read status changes
            var (realName, _) = await _systemInfoServices.GetRealName();
            if (!string.IsNullOrEmpty(realName))
            {
                await _hubContext.Clients.User(realName).SendAsync("MailsMarkedAsRead", new
                {
                    MailIds = mailIds,
                    UnreadCount = 0
                });
            }
        }

      

        /// <summary>
        /// Gets filtered mails based on various criteria with real-time notification
        /// </summary>
        /// <param name="filter">Filter criteria for mails</param>
        /// <returns>Response with filtered mail list</returns>
        public async Task<BaseResponseDTOs> GetFilteredMails(MailFilterViewForm filter)
        {
            try
            {
                var (realName, error) = await _systemInfoServices.GetRealName();
                if (!string.IsNullOrEmpty(error))
                    return new BaseResponseDTOs(null, 404, "User identification not available");

                // Build query with efficient filtering
                var query = _context.TFileTransferrings
                    .AsNoTracking()
                    .Where(m => m.To == realName);

                // Apply all filters as efficiently as possible
                if (filter.FromDate.HasValue)
                    query = query.Where(m => m.SendDate >= filter.FromDate.Value);

                if (filter.ToDate.HasValue)
                {
                    // Add one day to include the entire end date
                    var endDate = filter.ToDate.Value.AddDays(1).AddTicks(-1);
                    query = query.Where(m => m.SendDate <= endDate);
                }

                if (!string.IsNullOrEmpty(filter.ReferenceNo))
                    query = query.Where(m => m.RefrenceNo != null && m.RefrenceNo.Contains(filter.ReferenceNo));

                if (!string.IsNullOrEmpty(filter.Content))
                    query = query.Where(m => m.Notes != null && m.Notes.Contains(filter.Content));

                if (filter.IsRead.HasValue)
                    query = query.Where(m => m.Readed == (filter.IsRead.Value ? 1 : 0));

                // Handle sender filtering - we need to be careful with encryption
                List<string> matchingSenders = new();
                if (!string.IsNullOrEmpty(filter.Sender))
                {
                    // Get all senders for the user's mails first
                    var senders = await _context.TFileTransferrings
                        .AsNoTracking()
                        .Where(m => m.To == realName)
                        .Select(m => m.From)
                        .Distinct()
                        .ToListAsync();

                    // Check each sender by decrypting names
                    foreach (var sender in senders)
                    {
                        if (sender == null) continue;

                        string decryptedName = sender;
                        if (!string.IsNullOrEmpty(decryptedName) &&
                            decryptedName.Contains(filter.Sender, StringComparison.OrdinalIgnoreCase))
                        {
                            matchingSenders.Add(sender);
                        }
                    }

                    // Apply the sender filter with the matching encrypted names
                    if (matchingSenders.Any())
                        query = query.Where(m => matchingSenders.Contains(m.From));
                    else
                        return new BaseResponseDTOs(new { Items = Array.Empty<object>(), Count = 0 },
                            200, "No mails found with the specified sender");
                }

                // Get total count for pagination
                int totalCount = await query.CountAsync();

                // Apply pagination
                int pageNumber = filter.PageNumber > 0 ? filter.PageNumber : 1;
                int pageSize = filter.PageSize > 0 ? filter.PageSize : 20;

                // Get paged mails with efficient projection
                var mails = await query
                    .OrderByDescending(m => m.SendDate)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(m => new
                    {
                        m.Id,
                        m.RefrenceNo,
                        m.From,
                        m.To,
                        m.Notes,
                        m.SendDate,
                        m.Readed
                    })
                    .ToListAsync();

                if (!mails.Any())
                {
                    return new BaseResponseDTOs(new
                    {
                        Items = Array.Empty<object>(),
                        Count = 0,
                        TotalCount = totalCount,
                        PageNumber = pageNumber,
                        PageSize = pageSize
                    }, 200, "No mails found matching the filters");
                }

                // Process results using the existing document retrieval logic
                var refNosSet = mails
                    .Where(m => !string.IsNullOrEmpty(m.RefrenceNo))
                    .Select(m => m.RefrenceNo)
                    .Distinct()
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var documentsMap = await GetDocumentsWithSafetyAsync(refNosSet);
                var docTypeDict = await GetDocumentTypesAsync(documentsMap);

                // Build result with document information
                var result = new List<object>(mails.Count);
                foreach (var mail in mails)
                {
                    // Get document info if available
                    object doc = null;
                    if (!string.IsNullOrEmpty(mail.RefrenceNo) &&
                        documentsMap.TryGetValue(mail.RefrenceNo, out var docObj))
                    {
                        var docDyn = (dynamic)docObj;
                        string docTypeName = null;

                        if (docTypeDict.TryGetValue((int)docDyn.DocType, out var typeName))
                            docTypeName = typeName;

                        doc = new
                        {
                            docDyn.Id,
                            docDyn.RefrenceNo,
                            docDyn.DocNo,
                            docDyn.DocDate,
                            docDyn.DocType,
                            DocTypeName = docTypeName,
                            docDyn.Subject,
                            docDyn.ImgUrl
                        };
                    }

                    result.Add(new
                    {
                        Mail = new
                        {
                            mail.Id,
                            mail.RefrenceNo,
                            mail.From,
                            SenderName = mail.From,
                            mail.To,
                            mail.Notes,
                            mail.SendDate,
                            IsRead = mail.Readed == 1
                        },
                        Document = doc
                    });
                }

                var response = new
                {
                    Items = result,
                    Count = mails.Count,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                    Timestamp = DateTime.UtcNow
                };

                return new BaseResponseDTOs(response, 200);
            }
            catch (Exception ex)
            {
                return new BaseResponseDTOs(null, 500, $"Error retrieving filtered mails: {ex.Message}");
            }
        }

        // Helper for safely decrypting names
        private string TryDecryptName(string encryptedName)
        {
            if (string.IsNullOrEmpty(encryptedName))
                return null;

            try
            {
                return _encryptionServices?.DecryptString256Bit(encryptedName);
            }
            catch
            {
                return encryptedName;
            }
        }
    }
}