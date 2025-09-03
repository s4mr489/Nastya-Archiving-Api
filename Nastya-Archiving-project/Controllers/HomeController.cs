using DocumentFormat.OpenXml.Presentation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nastya_Archiving_project.Models.DTOs;
using Nastya_Archiving_project.Services.home;

namespace Nastya_Archiving_project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class HomeController : ControllerBase
    {
        private readonly IHomeServices _homeServices;

        public HomeController(IHomeServices homeServices)
        {
            _homeServices = homeServices;
        }

        /// <summary>
        /// Gets the list of active users who have performed actions in the system
        /// </summary>
        /// <returns>A list of active users with their activity details</returns>
        [HttpGet("active-users")]
        public async Task<IActionResult> GetActiveUsers()
        {
            var result = await _homeServices.ActiveUsers();

            if (result.StatusCode == 200)
                return Ok(result);

            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Get the counts of users 
        /// </summary>
        /// <returns></returns>
        [HttpGet("Users-Counts")]
        public async Task<IActionResult> GetUsersCount()
        {
            var result = await _homeServices.UsersCount();
            if (result.StatusCode == 200)
                return Ok(result);
            return StatusCode(result.StatusCode, result);
        }
        /// <summary>
        /// Gets the average number of documents processed per day
        /// </summary>
        /// <returns>Average document count per day and related statistics</returns>
        [HttpGet("docs-average-by-day")]
        public async Task<IActionResult> GetDocsAverageByDay()
        {
            var result = await _homeServices.DocsAvaregByDay();

            if (result.StatusCode == 200)
                return Ok(result);

            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Gets the count of branches in the system
        /// </summary>
        /// <returns>Total number of branches</returns>
        [HttpGet("branch-count")]
        public async Task<IActionResult> GetBranchCount()
        {
            var result = await _homeServices.BranchCount();

            if (result.StatusCode == 200)
                return Ok(result);

            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Gets the count of departments in the system
        /// </summary>
        /// <returns>Total number of departments</returns>
        [HttpGet("department-count")]
        public async Task<IActionResult> GetDepartmentCount()
        {
            var result = await _homeServices.DepartmentCount();

            if (result.StatusCode == 200)
                return Ok(result);

            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Gets the total size of all documents in the system
        /// </summary>
        /// <returns>Total document size in KB, MB, and GB with related metrics</returns>
        [HttpGet("total-docs-size")]
        public async Task<IActionResult> GetTotalDocsSize()
        {
            var result = await _homeServices.TotalDocsSize();

            if (result.StatusCode == 200)
                return Ok(result);

            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Gets the total count of documents in the system
        /// </summary>
        /// <returns>Total document count</returns>
        [HttpGet("docs-count")]
        public async Task<IActionResult> GetDocsCount()
        {
            var result = await _homeServices.DocsCount();

            if (result.StatusCode == 200)
                return Ok(result);

            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Gets dashboard statistics including various metrics
        /// </summary>
        /// <returns>Comprehensive system statistics for the dashboard</returns>
        //[HttpGet("dashboard-stats")]
        //public async Task<IActionResult> GetDashboardStats()
        //{
        //    // Get various statistics in parallel
        //    var activeUsersTask = _homeServices.ActiveUsers();
        //    var docsAverageTask = _homeServices.DocsAvaregByDay();
        //    var branchCountTask = _homeServices.BranchCount();
        //    var departmentCountTask = _homeServices.DepartmentCount();
        //    var totalDocsSizeTask = _homeServices.TotalDocsSize();
        //    var docsCountTask = _homeServices.DocsCount();

        //    // Wait for all tasks to complete
        //    await Task.WhenAll(activeUsersTask, docsAverageTask, branchCountTask, 
        //                      departmentCountTask, totalDocsSizeTask, docsCountTask);

        //    // Check if any of the tasks failed
        //    if (activeUsersTask.Result.StatusCode != 200 || 
        //        docsAverageTask.Result.StatusCode != 200 ||
        //        branchCountTask.Result.StatusCode != 200 ||
        //        departmentCountTask.Result.StatusCode != 200 ||
        //        totalDocsSizeTask.Result.StatusCode != 200 ||
        //        docsCountTask.Result.StatusCode != 200)
        //    {
        //        return StatusCode(500, new BaseResponseDTOs(null, 500, "Error retrieving one or more dashboard statistics"));
        //    }

        //    // Combine all results into a single response
        //    return Ok(new BaseResponseDTOs(
        //        new 
        //        { 
        //            activeUsers = activeUsersTask.Result.Data,
        //            docsAverage = docsAverageTask.Result.Data,
        //            branchCount = branchCountTask.Result.Data,
        //            departmentCount = departmentCountTask.Result.Data,
        //            totalDocsSize = totalDocsSizeTask.Result.Data,
        //            docsCount = docsCountTask.Result.Data
        //        },
        //        200,
        //        null
        //    ));
        //}


        /// <summary>
        /// Gets the total count of documents for the user by document type to spsific time frame
        /// </summary>
        /// <returns>Total document count</returns>f
        [HttpGet("user-docs-by-type")]
        public async Task<IActionResult> GetUserDocsByType([FromQuery] string timeFrame = null)
        {
            var result = await _homeServices.UserDocsByType(timeFrame);
            if (result.StatusCode == 200)
                return Ok(result);
            return StatusCode(result.StatusCode, result);
        }
    }
}