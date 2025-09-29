using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Nastya_Archiving_project.Models;
using Nastya_Archiving_project.Models.Entity;
using System;
using System.Collections.Generic;

namespace Nastya_Archiving_project.Data;

public partial class AppDbContext : DbContext
{
    private readonly IConfiguration _configuration;
    public AppDbContext(DbContextOptions<AppDbContext> options, IConfiguration configuration)
        : base(options)
    {
        _configuration = configuration;
    }

    // Extension method to get the configuration
    public IConfiguration GetConfiguration()
    {
        return _configuration;
    }

    public virtual DbSet<ArcivDocDscrp> ArcivDocDscrps { get; set; }

    public virtual DbSet<ArcivDocsRefrence> ArcivDocsRefrences { get; set; }

    public virtual DbSet<ArcivSubDocDscrp> ArcivSubDocDscrps { get; set; }

    public virtual DbSet<ArcivingDoc> ArcivingDocs { get; set; }

    public virtual DbSet<ArcivingDocsDeleted> ArcivingDocsDeleteds { get; set; }

    public virtual DbSet<GeneralOption> GeneralOptions { get; set; }

    public virtual DbSet<GpAccountingUnit> GpAccountingUnits { get; set; }

    public virtual DbSet<GpBranch> GpBranches { get; set; }

    public virtual DbSet<GpColumnsInfo> GpColumnsInfos { get; set; }

    public virtual DbSet<GpDepartment> GpDepartments { get; set; }

    public virtual DbSet<GpMonthDscrp> GpMonthDscrps { get; set; }

    public virtual DbSet<GpParametersPage> GpParametersPages { get; set; }

    public virtual DbSet<GpSubDepartment> GpSubDepartments { get; set; }

    public virtual DbSet<GpSystemImage> GpSystemImages { get; set; }

    public virtual DbSet<GpSystemImagesP> GpSystemImagesPs { get; set; }

    public virtual DbSet<GpTablesInfo> GpTablesInfos { get; set; }

    public virtual DbSet<GtCustomization> GtCustomizations { get; set; }

    public virtual DbSet<GtCustomizationsP> GtCustomizationsPs { get; set; }

    public virtual DbSet<PArciningMainFolder> PArciningMainFolders { get; set; }

    public virtual DbSet<PArcivingPoint> PArcivingPoints { get; set; }

    public virtual DbSet<PBlindMail> PBlindMails { get; set; }

    public virtual DbSet<PDefaultBody> PDefaultBodies { get; set; }

    public virtual DbSet<PDocsWay> PDocsWays { get; set; }

    public virtual DbSet<PFileType> PFileTypes { get; set; }

    public virtual DbSet<PJobTitle> PJobTitles { get; set; }

    public virtual DbSet<PMailAdress> PMailAdresses { get; set; }

    public virtual DbSet<PMailHost> PMailHosts { get; set; }

    public virtual DbSet<PMailSample> PMailSamples { get; set; }

    public virtual DbSet<PNotesSample> PNotesSamples { get; set; }

    public virtual DbSet<POperation> POperations { get; set; }

    public virtual DbSet<POrganization> POrganizations { get; set; }

    public virtual DbSet<PPackeg> PPackegs { get; set; }

    public virtual DbSet<TDocumentsRelation> TDocumentsRelations { get; set; }

    public virtual DbSet<TFileTransferring> TFileTransferrings { get; set; }

    public virtual DbSet<TMailArchiving> TMailArchivings { get; set; }

    public virtual DbSet<TMailTarget> TMailTargets { get; set; }

    public virtual DbSet<TWritedDocument> TWritedDocuments { get; set; }
    public virtual DbSet<T_JoinedDoc> JoinedDocs { get; set; }

    public virtual DbSet<TmpCorroptedDocument> TmpCorroptedDocuments { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UsersArchivingPointsPermission> UsersArchivingPointsPermissions { get; set; }

    public virtual DbSet<UsersDoctypePermission> UsersDoctypePermissions { get; set; }

    public virtual DbSet<UsersEditing> UsersEditings { get; set; }

    public virtual DbSet<UsersFileBrowing> UsersFileBrowings { get; set; }

    public virtual DbSet<UsersOptionPermission> UsersOptionPermissions { get; set; }

    public virtual DbSet<UsersPerm> UsersPerms { get; set; }

    public virtual DbSet<UsersReportColumn> UsersReportColumns { get; set; }

    public virtual DbSet<Usersgroup> Usersgroups { get; set; }

    public virtual DbSet<Usersinterface> Usersinterfaces { get; set; }

    public virtual DbSet<Userspermission> Userspermissions { get; set; }

    public virtual DbSet<WfPPrecedence> WfPPrecedences { get; set; }

    public virtual DbSet<WfPProcessType> WfPProcessTypes { get; set; }

    public virtual DbSet<WfProcessSequence> WfProcessSequences { get; set; }

    public virtual DbSet<WfTArchivingCreatedDoc> WfTArchivingCreatedDocs { get; set; }

    public virtual DbSet<WfTAttached> WfTAttacheds { get; set; }

    public virtual DbSet<WfTExportDocNo> WfTExportDocNos { get; set; }

    public virtual DbSet<WfTProcessing> WfTProcessings { get; set; }

    public virtual DbSet<WfTSample> WfTSamples { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            optionsBuilder.UseSqlServer(connectionString, options => options.EnableRetryOnFailure());
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("Arabic_100_CI_AI_KS_WS_SC");

        modelBuilder.Entity<ArcivDocDscrp>(entity =>
        {
            entity.ToTable("Arciv_DocDscrp");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccountUnitId).HasColumnName("AccountUnitID");
            entity.Property(e => e.BranchId).HasColumnName("BranchID");
            entity.Property(e => e.DepartId).HasColumnName("DepartID");
            entity.Property(e => e.Dscrp).HasMaxLength(50);
            entity.Property(e => e.IsoCode).HasMaxLength(50);
        });

        modelBuilder.Entity<ArcivDocsRefrence>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Docs_Refrences");

            entity.ToTable("Arciv_Docs_Refrences");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccountUnitId).HasColumnName("AccountUnitID");
            entity.Property(e => e.Dscription).HasMaxLength(150);
            entity.Property(e => e.EditDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Editor).HasMaxLength(50);
            entity.Property(e => e.HeadReferenceNo).HasMaxLength(50);
            entity.Property(e => e.LinkedRfrenceNo).HasMaxLength(50);
            entity.Property(e => e.PakegId).HasColumnName("PakegID");
        });

        modelBuilder.Entity<ArcivSubDocDscrp>(entity =>
        {
            entity.ToTable("Arciv_SubDocDscrp");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DocTypeId).HasColumnName("DocTypeID");
            entity.Property(e => e.Dscrp).HasMaxLength(50);
            entity.Property(e => e.accountUnitId).HasColumnName("AccountUnitID");
            entity.Property(e => e.branchId).HasColumnName("BranchID");
            entity.Property(e => e.departId).HasColumnName("DepartID");
        });

        modelBuilder.Entity<ArcivingDoc>(entity =>
        {
            entity.ToTable("Arciving_Docs");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccountUnitId).HasColumnName("AccountUnitID");
            entity.Property(e => e.BoxfileNo).HasMaxLength(50);
            entity.Property(e => e.BranchId).HasColumnName("BranchID");
            entity.Property(e => e.DepartId).HasColumnName("DepartID");
            entity.Property(e => e.DocId).HasColumnName("DocID");
            entity.Property(e => e.DocNo).HasMaxLength(50);
            entity.Property(e => e.DocSize).HasColumnType("numeric(10, 2)");
            entity.Property(e => e.DocTitle).HasMaxLength(250);
            entity.Property(e => e.EditDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Editor).HasMaxLength(50);
            entity.Property(e => e.Fourth)
                .HasMaxLength(250)
                .HasColumnName("fourth");
            entity.Property(e => e.HaseBakuped).HasDefaultValue(0);
            entity.Property(e => e.ImgUrl)
                .HasMaxLength(150)
                .HasColumnName("ImgURL");
            entity.Property(e => e.Ipaddress)
                .HasMaxLength(16)
                .HasColumnName("IPAddress");
            entity.Property(e => e.ReferenceTo).HasMaxLength(50);
            entity.Property(e => e.RefrenceNo).HasMaxLength(50);
            entity.Property(e => e.Sequre).HasColumnName("sequre");
            entity.Property(e => e.Subject).HasMaxLength(250);
            entity.Property(e => e.SystemId)
                .HasMaxLength(50)
                .HasColumnName("SystemID");
            entity.Property(e => e.TheMonth).HasComputedColumnSql("(datepart(month,[DocDate]))", false);
            entity.Property(e => e.Theyear)
                .HasComputedColumnSql("(datepart(year,[DocDate]))", false)
                .HasColumnName("theyear");
            entity.Property(e => e.WordsTosearch).HasMaxLength(500);
        });

        modelBuilder.Entity<ArcivingDocsDeleted>(entity =>
        {
            entity.ToTable("Arciving_Docs_Deleted");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.AccountUnitId).HasColumnName("AccountUnitID");
            entity.Property(e => e.BoxfileNo).HasMaxLength(50);
            entity.Property(e => e.BranchId).HasColumnName("BranchID");
            entity.Property(e => e.DepartId).HasColumnName("DepartID");
            entity.Property(e => e.DocDate).HasColumnType("datetime");
            entity.Property(e => e.DocId).HasColumnName("DocID");
            entity.Property(e => e.DocNo).HasMaxLength(50);
            entity.Property(e => e.DocSize).HasColumnType("numeric(10, 2)");
            entity.Property(e => e.DocTitle).HasMaxLength(250);
            entity.Property(e => e.EditDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Editor).HasMaxLength(50);
            entity.Property(e => e.Fourth)
                .HasMaxLength(250)
                .HasColumnName("fourth");
            entity.Property(e => e.HaseBakuped).HasDefaultValue(0);
            entity.Property(e => e.ImgUrl)
                .HasMaxLength(150)
                .HasColumnName("ImgURL");
            entity.Property(e => e.Ipaddress)
                .HasMaxLength(16)
                .HasColumnName("IPAddress");
            entity.Property(e => e.RefrenceNo).HasMaxLength(50);
            entity.Property(e => e.Sequre).HasColumnName("sequre");
            entity.Property(e => e.Subject).HasMaxLength(250);
            entity.Property(e => e.SystemId)
                .HasMaxLength(50)
                .HasColumnName("SystemID");
            entity.Property(e => e.TheMonth).HasComputedColumnSql("(datepart(month,[DocDate]))", false);
            entity.Property(e => e.Theyear)
                .HasComputedColumnSql("(datepart(year,[DocDate]))", false)
                .HasColumnName("theyear");
            entity.Property(e => e.WordsTosearch).HasMaxLength(500);
        });

        modelBuilder.Entity<GeneralOption>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.EnyCript)
                .HasMaxLength(50)
                .HasColumnName("EnyCript_");
            entity.Property(e => e.Language)
                .HasMaxLength(50)
                .HasDefaultValueSql("((0))")
                .HasColumnName("Language_");
        });

        modelBuilder.Entity<GpAccountingUnit>(entity =>
        {
            entity.ToTable("gp_AccountingUnits");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Dscrp).HasMaxLength(30);
            entity.Property(e => e.StorePath).HasMaxLength(250);
            entity.Property(e => e.BackupPath).HasMaxLength(250);
        });

        modelBuilder.Entity<GpBranch>(entity =>
        {
            entity.ToTable("gp_Branches");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.AccountUnitId).HasColumnName("AccountUnitID");
            entity.Property(e => e.Dscrp).HasMaxLength(88);
            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("id");
        });

        modelBuilder.Entity<GpColumnsInfo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_users_ReportColumns");

            entity.ToTable("Gp_ColumnsInfo");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccountUnitId).HasColumnName("AccountUnitID");
            entity.Property(e => e.BranchId).HasColumnName("BranchID");
            entity.Property(e => e.DataType).HasMaxLength(50);
            entity.Property(e => e.DepartmentId).HasColumnName("DepartmentID");
            entity.Property(e => e.Dscrp).HasMaxLength(50);
            entity.Property(e => e.DscrpA).HasMaxLength(50);
            entity.Property(e => e.ParameterTable)
                .HasMaxLength(250)
                .HasDefaultValue("-");
            entity.Property(e => e.Required).HasDefaultValue(0);
            entity.Property(e => e.TblName).HasMaxLength(50);
            entity.Property(e => e.TheSystem).HasMaxLength(50);
            entity.Property(e => e.UserInserted).HasDefaultValue(0);
        });

        modelBuilder.Entity<GpDepartment>(entity =>
        {
            entity.ToTable("gp_Departments");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccountUnitId).HasColumnName("AccountUnitID");
            entity.Property(e => e.BranchId).HasColumnName("BranchID");
            entity.Property(e => e.Dscrp).HasMaxLength(88);
        });

        modelBuilder.Entity<GpMonthDscrp>(entity =>
        {
            entity.ToTable("gp_MonthDscrp");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Monthdscrp).HasMaxLength(20);
        });

        modelBuilder.Entity<GpParametersPage>(entity =>
        {
            entity.ToTable("gp_ParametersPages");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Dscrp).HasMaxLength(50);
            entity.Property(e => e.Modl).HasMaxLength(50);
            entity.Property(e => e.Url)
                .HasMaxLength(150)
                .HasColumnName("URL");
        });

        modelBuilder.Entity<GpSubDepartment>(entity =>
        {
            entity.ToTable("gp_SubDepartments");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccountUnitId).HasColumnName("AccountUnitID");
            entity.Property(e => e.BranchId).HasColumnName("BranchID");
            entity.Property(e => e.DepartmentId).HasColumnName("DepartmentID");
            entity.Property(e => e.Dscrp).HasMaxLength(50);
        });

        modelBuilder.Entity<GpSystemImage>(entity =>
        {
            entity.ToTable("Gp_SystemImages");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccountUnitId).HasColumnName("AccountUnitID");
            entity.Property(e => e.Dscrp).HasMaxLength(50);
            entity.Property(e => e.Img)
                .HasColumnType("image")
                .HasColumnName("img");
        });

        modelBuilder.Entity<GpSystemImagesP>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("Gp_SystemImagesP");

            entity.Property(e => e.Dscrp).HasMaxLength(50);
            entity.Property(e => e.DscrpA).HasMaxLength(50);
            entity.Property(e => e.Id).HasColumnName("id");
        });

        modelBuilder.Entity<GpTablesInfo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Table_1");

            entity.ToTable("Gp_TablesInfo");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Dscrp).HasMaxLength(50);
            entity.Property(e => e.DscrpA).HasMaxLength(50);
            entity.Property(e => e.TblType).HasMaxLength(50);
        });

        modelBuilder.Entity<GtCustomization>(entity =>
        {
            entity.ToTable("gt_Customizations");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccountUnitId).HasColumnName("AccountUnitID");
            entity.Property(e => e.BranchId).HasColumnName("BranchID");
            entity.Property(e => e.DepartId).HasColumnName("DepartID");
            entity.Property(e => e.TheTable).HasMaxLength(50);
            entity.Property(e => e.TheTableA).HasMaxLength(50);
            entity.Property(e => e.TheType).HasMaxLength(10);
            entity.Property(e => e.TheValue).HasMaxLength(10);
        });

        modelBuilder.Entity<GtCustomizationsP>(entity =>
        {
            entity.ToTable("gt_CustomizationsP");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.TheSystem).HasMaxLength(50);
            entity.Property(e => e.TheTable).HasMaxLength(50);
            entity.Property(e => e.TheTableA).HasMaxLength(50);
            entity.Property(e => e.TheType).HasMaxLength(50);
        });

        modelBuilder.Entity<PArciningMainFolder>(entity =>
        {
            entity.ToTable("P_ArciningMainFolder");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.StorePath).HasMaxLength(250);
        });

        modelBuilder.Entity<PArcivingPoint>(entity =>
        {
            entity.ToTable("P_ArcivingPoints");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccountUnitId).HasColumnName("AccountUnitID");
            entity.Property(e => e.BackupPath).HasMaxLength(250);
            entity.Property(e => e.BranchId).HasColumnName("BranchID");
            entity.Property(e => e.DepartId).HasColumnName("DepartID");
            entity.Property(e => e.Dscrp).HasMaxLength(50);
            entity.Property(e => e.StartWith).HasMaxLength(50);
            entity.Property(e => e.StorePath).HasMaxLength(250);
        });

        modelBuilder.Entity<PBlindMail>(entity =>
        {
            entity.ToTable("P_BlindMail");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Dscrp).HasMaxLength(250);
        });

        modelBuilder.Entity<PDefaultBody>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("P_DefaultBody");
        });

        modelBuilder.Entity<PDocsWay>(entity =>
        {
            entity.ToTable("P_DocsWay");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Dscrp).HasMaxLength(20);
        });

        modelBuilder.Entity<PFileType>(entity =>
        {
            entity.ToTable("P_FileType");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Dscrp).HasMaxLength(50);
            entity.Property(e => e.Extention).HasMaxLength(50);
        });

        modelBuilder.Entity<PJobTitle>(entity =>
        {
            entity.ToTable("P_JobTitle");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Dscrp).HasMaxLength(150);
            entity.Property(e => e.StepId).HasColumnName("StepID");
        });

        modelBuilder.Entity<PMailAdress>(entity =>
        {
            entity.ToTable("P_MailAdresses");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccountUnitId).HasColumnName("AccountUnitID");
            entity.Property(e => e.BranchId).HasColumnName("BranchID");
            entity.Property(e => e.DepartmentId).HasColumnName("DepartmentID");
            entity.Property(e => e.Dscrp).HasMaxLength(50);
            entity.Property(e => e.Email).HasMaxLength(250);
        });

        modelBuilder.Entity<PMailHost>(entity =>
        {
            entity.ToTable("P_MailHosts");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Dscrp).HasMaxLength(50);
            entity.Property(e => e.EMail)
                .HasMaxLength(250)
                .HasColumnName("E_Mail");
            entity.Property(e => e.EnabledSsl)
                .HasMaxLength(50)
                .HasColumnName("EnabledSSL");
            entity.Property(e => e.Pass).HasMaxLength(50);
            entity.Property(e => e.ServerDomen).HasMaxLength(250);
        });

        modelBuilder.Entity<PMailSample>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_P_Samples");

            entity.ToTable("P_Mail_Samples");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Dscrp).HasMaxLength(500);
            entity.Property(e => e.Subject).HasMaxLength(50);
            entity.Property(e => e.UserId).HasColumnName("UserID");
        });

        modelBuilder.Entity<PNotesSample>(entity =>
        {
            entity.ToTable("P_Notes_Samples");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Dscrp).HasMaxLength(50);
            entity.Property(e => e.Notes).HasMaxLength(250);
            entity.Property(e => e.UserId).HasColumnName("UserID");
        });

        modelBuilder.Entity<POperation>(entity =>
        {
            entity.ToTable("P_Operation");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Dscrp).HasMaxLength(50);
        });

        modelBuilder.Entity<POrganization>(entity =>
        {
            entity.ToTable("P_Organizations");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccountUnitId).HasColumnName("AccountUnitID");
            entity.Property(e => e.BranchId).HasColumnName("BranchID");
            entity.Property(e => e.DepartId).HasColumnName("DepartID");
            entity.Property(e => e.Dscrp).HasMaxLength(50);
        });

        modelBuilder.Entity<PPackeg>(entity =>
        {
            entity.ToTable("P_Packegs");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Dscrp).HasMaxLength(150);
            entity.Property(e => e.status).HasDefaultValue(0);
        });

        modelBuilder.Entity<TDocumentsRelation>(entity =>
        {
            entity.ToTable("T_DocumentsRelations");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ParentDocId).HasColumnName("ParentDocID");
            entity.Property(e => e.RelatedDocId).HasColumnName("RelatedDocID");
        });

        modelBuilder.Entity<TFileTransferring>(entity =>
        {
            entity.ToTable("T_FileTransferring");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.From)
                .HasMaxLength(50)
                .HasColumnName("FROM_");
            entity.Property(e => e.Notes).HasMaxLength(250);
            entity.Property(e => e.Readed).HasDefaultValue(0);
            entity.Property(e => e.RefrenceNo).HasMaxLength(50);
            entity.Property(e => e.SendDate).HasColumnType("datetime");
            entity.Property(e => e.To)
                .HasMaxLength(50)
                .HasColumnName("TO_");
        });

        modelBuilder.Entity<TMailArchiving>(entity =>
        {
            entity.ToTable("T_MailArchiving");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccountUnitId).HasColumnName("AccountUnitID");
            entity.Property(e => e.BodyStr).HasMaxLength(250);
            entity.Property(e => e.Ccto)
                .HasMaxLength(250)
                .HasColumnName("CCto");
            entity.Property(e => e.FileUrl)
                .HasMaxLength(50)
                .HasColumnName("FileURL");
            entity.Property(e => e.SendTo).HasMaxLength(250);
            entity.Property(e => e.SenderName).HasMaxLength(50);
            entity.Property(e => e.Subject).HasMaxLength(50);
        });

        modelBuilder.Entity<TMailTarget>(entity =>
        {
            entity.ToTable("T_MailTargets");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DepartmentId).HasColumnName("DepartmentID");
            entity.Property(e => e.Username).HasMaxLength(50);
            entity.Property(e => e.UsertargetMail)
                .HasMaxLength(150)
                .HasColumnName("usertargetMail");
        });

        modelBuilder.Entity<TWritedDocument>(entity =>
        {
            entity.ToTable("T_Writed_Documents");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccountUnitId).HasColumnName("AccountUnitID");
            entity.Property(e => e.BoxfileNo).HasMaxLength(50);
            entity.Property(e => e.BranchId).HasColumnName("BranchID");
            entity.Property(e => e.DepartId).HasColumnName("DepartID");
            entity.Property(e => e.DocNo).HasMaxLength(20);
            entity.Property(e => e.EditDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Editor).HasMaxLength(50);
            entity.Property(e => e.Subject).HasMaxLength(250);
            entity.Property(e => e.TheMonth).HasComputedColumnSql("(datepart(month,[DocDate]))", false);
            entity.Property(e => e.Theyear)
                .HasComputedColumnSql("(datepart(year,[DocDate]))", false)
                .HasColumnName("theyear");
        });

        modelBuilder.Entity<TmpCorroptedDocument>(entity =>
        {
            entity.ToTable("TMP_CorroptedDocuments");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.TheState).HasMaxLength(50);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccountUnitId).HasColumnName("AccountUnitID");
            entity.Property(e => e.Adminst)
                .HasMaxLength(172)
                .HasColumnName("adminst");
            entity.Property(e => e.AsWfuser)
                .HasDefaultValue(0)
                .HasColumnName("AsWFUser");
            entity.Property(e => e.AsmailCenter).HasColumnName("ASMailCenter");
            entity.Property(e => e.BranchId).HasColumnName("BranchID");
            entity.Property(e => e.DepariId).HasColumnName("DepariID");
            entity.Property(e => e.DevisionId).HasColumnName("DevisionID");
            entity.Property(e => e.Editor).HasMaxLength(172);
            entity.Property(e => e.GroupId).HasColumnName("GroupID");
            entity.Property(e => e.Stoped).HasDefaultValue(0)
                                          .HasColumnName("Stoped");
            entity.Property(e => e.Permtype)
                .HasMaxLength(172)
                .HasColumnName("permtype");
            entity.Property(e => e.Realname).HasMaxLength(172);
            entity.Property(e => e.Email)
            .HasMaxLength(100)
            .HasColumnName("Email");
            entity.Property(e => e.PhoneNo)
            .HasMaxLength(20)
            .HasColumnName("PhoneNo");
            entity.Property(e => e.Address)
            .HasColumnName("Adress_")
            .HasMaxLength(50);

            entity.Property(e => e.UserName).HasMaxLength(172);
            entity.Property(e => e.UserPassword).HasMaxLength(172);
        });

        modelBuilder.Entity<UsersArchivingPointsPermission>(entity =>
        {
            entity.ToTable("Users_ArchivingPointsPermissions");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccountUnitId).HasColumnName("AccountUnitID");
            entity.Property(e => e.ArchivingpointId).HasColumnName("ArchivingpointID");
            entity.Property(e => e.DepartId).HasColumnName("DepartID");
            entity.Property(e => e.UserId).HasColumnName("UserID");
        });

        modelBuilder.Entity<UsersDoctypePermission>(entity =>
        {
            entity.ToTable("users_Doctype_Permissions");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DoctypeId).HasColumnName("DoctypeID");
            entity.Property(e => e.UserId).HasColumnName("UserID");
        });

        modelBuilder.Entity<UsersEditing>(entity =>
        {
            entity.ToTable("Users_Editing");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccountUnitId).HasColumnName("AccountUnitID");
            entity.Property(e => e.EditDate).HasColumnType("datetime");
            entity.Property(e => e.Editor).HasMaxLength(50);
            entity.Property(e => e.Ipadress)
                .HasMaxLength(20)
                .HasColumnName("IPAdress");
            entity.Property(e => e.Model).HasMaxLength(10);
            entity.Property(e => e.OperationType).HasMaxLength(10);
            entity.Property(e => e.RecordData).HasMaxLength(1024);
            entity.Property(e => e.RecordId)
                .HasMaxLength(20)
                .HasColumnName("RecordID");
            entity.Property(e => e.TblName).HasMaxLength(20);
            entity.Property(e => e.TblNameA).HasMaxLength(20);
        });

        modelBuilder.Entity<UsersFileBrowing>(entity =>
        {
            entity.ToTable("Users_FileBrowing");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("UserID");
        });

        modelBuilder.Entity<UsersOptionPermission>(entity =>
        {
            entity.ToTable("Users_OptionPermissions");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AddParameters).HasDefaultValue(0);
            entity.Property(e => e.AllowAddToOther).HasDefaultValue(0);
            entity.Property(e => e.AllowDelete).HasDefaultValue(0);
            entity.Property(e => e.AllowDownload).HasDefaultValue(0);
            entity.Property(e => e.AllowSendMail).HasDefaultValue(0);
            entity.Property(e => e.AllowViewTheOther).HasDefaultValue(0);
            entity.Property(e => e.UserId).HasColumnName("UserID");
        });

        modelBuilder.Entity<UsersPerm>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("UsersPerm");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.PermId).HasColumnName("PermID");
            entity.Property(e => e.PermType).HasMaxLength(20);
        });

        modelBuilder.Entity<UsersReportColumn>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_users_ReportColumns_1");

            entity.ToTable("users_ReportColumns");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccountUnitId).HasColumnName("AccountUnitID");
            entity.Property(e => e.Asparameter)
                .HasDefaultValue(0)
                .HasColumnName("ASParameter");
            entity.Property(e => e.BranchId).HasColumnName("BranchID");
            entity.Property(e => e.ColumnName).HasMaxLength(100);
            entity.Property(e => e.ColumnNamedisplay).HasMaxLength(150);
            entity.Property(e => e.ConStr).HasMaxLength(1000);
            entity.Property(e => e.ControlType).HasMaxLength(50);
            entity.Property(e => e.DataType).HasMaxLength(50);
            entity.Property(e => e.DepartmentId).HasColumnName("DepartmentID");
            entity.Property(e => e.JoiningPart)
                .HasMaxLength(1000)
                .HasDefaultValue("-")
                .HasColumnName("joiningPart");
            entity.Property(e => e.ParameterTable)
                .HasMaxLength(150)
                .HasDefaultValue("-");
            entity.Property(e => e.PrimeryKey).HasDefaultValue(0);
            entity.Property(e => e.Required).HasDefaultValue(0);
            entity.Property(e => e.SecondaryKey)
                .HasMaxLength(150)
                .HasDefaultValue("-");
            entity.Property(e => e.SecondaryTable)
                .HasMaxLength(150)
                .HasDefaultValue("-");
            entity.Property(e => e.TblName).HasMaxLength(150);
            entity.Property(e => e.TblNameA).HasMaxLength(150);
            entity.Property(e => e.TheSystem).HasMaxLength(50);
            entity.Property(e => e.UserInserted).HasDefaultValue(0);
        });

        //modelBuilder.Entity<Usersgroup>(entity =>
        //{
        //    entity.ToTable("usersgroups");

        //    entity.Property(e => e.groupid).HasColumnName("groupid");
        //    entity.Property(e => e.AccountUnitId).HasColumnName("AccountUnitID");
        //    entity.Property(e => e.Editor).HasMaxLength(172);
        //    entity.Property(e => e.Groupdscrp)
        //        .HasMaxLength(50)
        //        .HasColumnName("groupdscrp");
        //});
        modelBuilder.Entity<Usersgroup>(entity =>
        {
            entity.ToTable("usersgroups");

            entity.Property(e => e.groupid).HasColumnName("groupid");
            entity.Property(e => e.AccountUnitId).HasColumnName("AccountUnitID");
            entity.Property(e => e.Editor).HasMaxLength(172);
            entity.Property(e => e.Groupdscrp)
                .HasMaxLength(50)
                .HasColumnName("groupdscrp");
            entity.Property(e => e.AllowDownload).HasDefaultValue(0);
            entity.Property(e => e.AllowSendMail).HasDefaultValue(0);
            entity.Property(e => e.AllowViewTheOther).HasDefaultValue(0);
            entity.Property(e => e.AllowAddToOther).HasDefaultValue(0);
            entity.Property(e => e.AllowDelete).HasDefaultValue(0);
            entity.Property(e => e.AddParameters).HasDefaultValue(0);
        });

        modelBuilder.Entity<Usersinterface>(entity =>
        {
            entity.ToTable("usersinterfacesNew");

            entity.Property(e => e.Id).HasColumnName("pageid");
            entity.Property(e => e.AccountUnitId).HasColumnName("AccountUnitID");
            entity.Property(e => e.Outputtype)
                .HasMaxLength(20)
                .HasColumnName("outputtype");
            entity.Property(e => e.outputTypeName)
                .HasMaxLength(20)
                .HasColumnName("outputTypeName");
            entity.Property(e => e.Pagedscrp)
                .HasMaxLength(50)
                .HasColumnName("pagedscrp");
            entity.Property(e => e.Pageurl)
                .HasMaxLength(100)
                .HasColumnName("pageurl");
            entity.Property(e => e.Program)
                .HasMaxLength(14)
                .HasColumnName("program");
        });

        modelBuilder.Entity<Userspermission>(entity =>
        {
            entity.ToTable("userspermissions");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccountUnitId).HasColumnName("AccountUnitID");
            entity.Property(e => e.Groupid).HasColumnName("groupid");
            entity.Property(e => e.Pageid)
                .HasMaxLength(50)
                .HasColumnName("pageid");
        });

        modelBuilder.Entity<WfPPrecedence>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_P_Precedence");

            entity.ToTable("WF_P_Precedence");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Dscrp).HasMaxLength(50);
        });

        modelBuilder.Entity<WfPProcessType>(entity =>
        {
            entity.ToTable("WF_P_ProcessType");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Dscrp).HasMaxLength(50);
        });

        modelBuilder.Entity<WfProcessSequence>(entity =>
        {
            entity.ToTable("WF_ProcessSequence");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Approved).HasDefaultValue(0);
            entity.Property(e => e.CaseId).HasColumnName("CaseID");
            entity.Property(e => e.Cc)
                .HasDefaultValue(0)
                .HasColumnName("CC");
            entity.Property(e => e.FromUserId).HasColumnName("FromUserID");
            entity.Property(e => e.FromUserName).HasMaxLength(150);
            entity.Property(e => e.Notes).HasMaxLength(512);
            entity.Property(e => e.OpeninigDate).HasColumnType("datetime");
            entity.Property(e => e.Redirected).HasDefaultValue(0);
            entity.Property(e => e.SendDate).HasColumnType("datetime");
            entity.Property(e => e.Signature).HasColumnType("image");
            entity.Property(e => e.ToUserId).HasColumnName("ToUserID");
            entity.Property(e => e.ToUserName).HasMaxLength(150);
            entity.Property(e => e.UnderHand).HasDefaultValue(0);
        });

        modelBuilder.Entity<WfTArchivingCreatedDoc>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Archiving_CreatedDocs");

            entity.ToTable("WF_T_ArchivingCreatedDocs");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccountUnitId).HasColumnName("AccountUnitID");
            entity.Property(e => e.BranchId).HasColumnName("BranchID");
            entity.Property(e => e.CaseId).HasColumnName("CaseID");
            entity.Property(e => e.DepartId).HasColumnName("DepartID");
            entity.Property(e => e.DocId).HasColumnName("DocID");
            entity.Property(e => e.DocIdtype)
                .HasMaxLength(4)
                .HasDefaultValue("_د")
                .HasColumnName("DocIDType");
            entity.Property(e => e.DocNo).HasMaxLength(50);
            entity.Property(e => e.EditDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Editor).HasMaxLength(50);
            entity.Property(e => e.ImgUrl)
                .HasMaxLength(250)
                .HasColumnName("ImgURL");
            entity.Property(e => e.Ipaddress)
                .HasMaxLength(16)
                .HasColumnName("IPAddress");
            entity.Property(e => e.SourceId).HasColumnName("SourceID");
            entity.Property(e => e.Subject).HasMaxLength(100);
            entity.Property(e => e.TargetId).HasColumnName("TargetID");
            entity.Property(e => e.Themonth)
                .HasComputedColumnSql("(datepart(month,[DocDate]))", false)
                .HasColumnName("themonth");
            entity.Property(e => e.Theyear)
                .HasComputedColumnSql("(datepart(year,[DocDate]))", false)
                .HasColumnName("theyear");
        });

        modelBuilder.Entity<WfTAttached>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_WF_P_Documents");

            entity.ToTable("WF_T_Attached");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CaseId).HasColumnName("CaseID");
            entity.Property(e => e.FileNam).HasMaxLength(150);
            entity.Property(e => e.ImgUrl)
                .HasMaxLength(250)
                .HasColumnName("ImgURL");
            entity.Property(e => e.RefrenceNo).HasMaxLength(20);
            entity.Property(e => e.UserId).HasColumnName("UserID");
        });

        modelBuilder.Entity<WfTExportDocNo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_T_ExportDocNo");

            entity.ToTable("WF_T_ExportDocNo");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccountUnitId).HasColumnName("AccountUnitID");
            entity.Property(e => e.Editor).HasMaxLength(50);
            entity.Property(e => e.ExportDocNo)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.ProcessTypeId).HasColumnName("ProcessTypeID");
            entity.Property(e => e.Theyear)
                .HasComputedColumnSql("(datepart(year,[EditDate]))", false)
                .HasColumnName("theyear");
        });

        modelBuilder.Entity<WfTProcessing>(entity =>
        {
            entity.ToTable("WF_T_Processing");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccountUnitId).HasColumnName("AccountUnitID");
            entity.Property(e => e.BranchId).HasColumnName("BranchID");
            entity.Property(e => e.CaseClosed).HasDefaultValue(0);
            entity.Property(e => e.CaseCreatedBy).HasColumnName("CaseCreatedBY");
            entity.Property(e => e.CaseId).HasColumnName("CaseID");
            entity.Property(e => e.DepartId).HasColumnName("DepartID");
            entity.Property(e => e.DocId).HasColumnName("DocID");
            entity.Property(e => e.DocNo).HasMaxLength(10);
            entity.Property(e => e.Notes).HasMaxLength(250);
            entity.Property(e => e.PrecedenceId).HasColumnName("PrecedenceID");
            entity.Property(e => e.ProcessType).HasDefaultValue(0);
            entity.Property(e => e.RefrenceNo).HasMaxLength(20);
            entity.Property(e => e.Subject).HasMaxLength(50);
            entity.Property(e => e.Themonth)
                .HasComputedColumnSql("(datepart(month,[DocDate]))", false)
                .HasColumnName("themonth");
            entity.Property(e => e.Theyear)
                .HasComputedColumnSql("(datepart(year,[DocDate]))", false)
                .HasColumnName("theyear");
        });

        modelBuilder.Entity<WfTSample>(entity =>
        {
            entity.ToTable("WF_T_Samples");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccountUnitId).HasColumnName("AccountUnitID");
            entity.Property(e => e.DepartId).HasColumnName("DepartID");
            entity.Property(e => e.Dscrp).HasMaxLength(50);
            entity.Property(e => e.Structures).HasMaxLength(512);
        });


        modelBuilder.Entity<T_JoinedDoc>(entity =>
        {
            entity.ToTable("T_JoinedDoc"); // <-- Changed from "JoinedDocs" to "T_JoinedDoc"
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.ParentRefrenceNO).HasMaxLength(100);
            entity.Property(e => e.ChildRefrenceNo).HasMaxLength(100);
            entity.Property(e => e.BreafcaseNo);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
