using HDVI.Core.MRCS.Common;
using HDVI.Core.MRCS.DataContract.Catalytic;
using HDVI.Core.MRCS.DataContract.Chase;
using HDVI.Core.MRCS.DataContract.Entity;
using HDVI.Core.MRCS.DataContract.Enums;
using HDVI.Core.MRCS.DataContract.EzDI;
using HDVI.Core.MRCS.DataContract.Member;
using HDVI.Core.MRCS.DataContract.OCR;
using HDVI.Core.MRCS.DataContract.Reporting;
using HDVI.Core.MRCS.ServiceContract.Chase;
using HDVI.Core.MRCS.ServiceContract.Pend;
using HDVI.Core.MRCS.Web.API.ViewModels.Chase;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace HDVI.Core.MRCS.Web.API.Controllers.Chase
{
    /// <summary>
    /// Controller to work with all Chase related APIs
    /// </summary>
    [SetLogInUserData]
    [Produces("application/json")]
    public class ChaseController : BaseController
    {
        protected readonly IChaseService _chaseService;
        protected readonly IPendService _pendService;
        protected readonly IChaseCommentService _chaseCommentService;
        protected readonly IChaseDocumentService _chaseDocumentService;


        /// <summary>
        /// Controller to work with all authentication related APIs
        /// </summary>
        /// <param name="chaseService"></param>
        /// <param name="pendService"></param>
        /// <param name="chaseCommentService"></param>
        /// <param name="chaseDocumentService"><</param>
        public ChaseController(IChaseService chaseService, IPendService pendService, IChaseCommentService chaseCommentService, IChaseDocumentService chaseDocumentService)
            : base(chaseService)
        {
            _chaseService = chaseService;

            _pendService = pendService;

            _chaseCommentService = chaseCommentService;

            _chaseDocumentService = chaseDocumentService;

        }

        /// <summary>
        /// This API is used to do a healthcheck on the Chase Controller.
        /// </summary>
        /// <returns>HealthCheck Information</returns>
        // GET: api/Chase/HealthCheck/
        [HttpGet]
        [Route("/api/Chase/HealthCheck")]
        [HandleException]
        [Obsolete]
        public async Task<IActionResult> HealthCheckAsync()
        {
            var result = await _chaseService.HealthCheckAsync();
            var response = Ok(result);

            return response;
        }

        /// <summary>
        /// This API is used to get the Metadata for all the components/sections of Chase Detail Page.
        /// </summary>
        /// <returns>Collection of Metadata Items to de displayed</returns>
        // GET: api/Chase/ChaseDetailsMetadata/
        [HttpGet]
        [Route("/api/Chase/ChaseDetailMetadata")]
        [HandleException]
        public async Task<IActionResult> GetChaseDetailsMetadataAsync()
        {
            IActionResult response = null;

            var chaseDetailMetadataList = new List<ChaseDetailMetadata>();
            await Task.Run(() => chaseDetailMetadataList.AddRange(_chaseService.GetChaseSummaryTimelineMetadata()));
            chaseDetailMetadataList.AddRange(_pendService.GetPendMetadataForChaseDetail());
            chaseDetailMetadataList.AddRange(_chaseCommentService.GetChaseCommentMetadata());
            chaseDetailMetadataList.AddRange(_chaseDocumentService.GetChartPreviewMetadataForChaseDetailAsync().Result);
            response = Ok(chaseDetailMetadataList);

            return response;
        }

        /// <summary>
        /// This API is used to get the chase summary based on chaseID.
        /// </summary>
        /// <param name="chaseId"></param>
        /// <returns>Chase Details</returns>
        // GET: api/Chase?chaseId=1234
        [HttpGet]
        [Route("/api/Chase")]
        [HandleException] // TODO: Fix
        public async Task<IActionResult> GetChaseDetailAsync(int chaseId)
        {
            IActionResult response = null;

            var chaseDetail = await _chaseService.GetChaseSummaryAsync(chaseId, _chaseService.LoginUser.UserId);
            response = Ok(chaseDetail);

            return response;
        }

        /// <summary>
        /// This API is used to get the chase detail.
        /// </summary>
        /// <param name="chaseId"></param>
        /// <returns>Chase Detail</returns>
        [HttpGet]
        [Route("/api/chase/detail")]
        [HandleException]
        public async Task<IActionResult> GetChaseDetail2Async(int chaseId)
        {
            IActionResult response = null;

            var chaseDetail = await _chaseService.GetChaseDetailAsync(chaseId, _chaseService.LoginUser.UserId, _chaseService.LoginUser.OrganizationId);
            response = Ok(chaseDetail);

            return response;
        }

        /// <summary>
        /// This API is used to get archived chase detail
        /// </summary>
        /// <param name="chaseId"></param>
        /// <returns>Archived Chase Detail</returns>
        [HttpGet]
        [Route("/api/chase/archive")]
        [HandleException]
        public async Task<IActionResult> GetChaseArchiveAsync(int chaseId)
        {
            IActionResult response = null;

            var chaseArchive = await _chaseService.GetChaseArchiveAsync(chaseId, _chaseService.LoginUser.UserId);
            response = Ok(chaseArchive);
            
            return response;
        }

        /// <summary>
        /// This API is used to get the chase comments based on chaseID.
        /// </summary>
        /// <param name="chaseId"></param>
        /// <param name="isOnlyLatest"></param>
        /// <returns>Chase Comments</returns>
        // GET: api/Chase/Comment?chaseId=1234&isOnlyLatest=true
        [HttpGet]
        [Route("/api/Chase/Comment")]
        [HandleException]
        public async Task<IActionResult> GetChaseCommentAsync(int chaseId, bool isOnlyLatest)
        {
            IActionResult response = null;

            var chaseComment = await _chaseService.GetChaseCommentsAsync(chaseId, isOnlyLatest);
            response = Ok(chaseComment);

            return response;
        }

        /// <summary>
        /// This API is used to get the chase & pend comments based on chaseID.
        /// </summary>
        /// <param name="chaseId"></param>
        /// <returns>Chase & Pend Comments</returns>
        // GET: api/chase/comments?chaseId=1234
        [HttpGet]
        [Route("/api/chase/comments")]
        [HandleException]
        public async Task<IActionResult> GetChasePendCommentsAsync(int chaseId)
        {
            IActionResult response = null;

            var chaseComment = await _chaseService.GetChasePendCommentsAsync(chaseId, _chaseService.LoginUser.UserId);
            response = Ok(chaseComment);

            return response;
        }

        /// <summary>
        /// This API is used to save the chase comment based on chaseID.
        /// </summary>
        /// <param name="chaseId"></param>
        /// <param name="commentText"><</param>
        /// <returns></returns>
        // POST: api/Chase/Comment?chaseId=1234&commentText='testComment'
        [HttpPost]
        [Route("/api/Chase/Comment")]
        [HandleException]
        public async Task<IActionResult> SaveChaseCommentAsync(int chaseId, string commentText)
        {
            IActionResult response = null;

            await _chaseService.AddChaseCommentAsync(chaseId, commentText, _chaseService.LoginUser.UserId);
            response = Ok();

            return response;
        }

        /// <summary>
        /// This API will select Chase data by filter criteria 
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("/api/chase/list")]
        public async Task<IActionResult> ChaseSearchAsync([FromBody] ChaseSearchCriteria searchCriteria)
        {
            IActionResult response = null;

            try
            {
                searchCriteria.CallerUserId = _chaseService.LoginUser.UserId;
                var chaseSearchResults = await _chaseService.ChaseSearchAsync(searchCriteria);

                response = Ok(chaseSearchResults);

            }
            catch (UnauthorizedAccessException)
            {
                response = StatusCode((int)HttpStatusCode.Forbidden);
            }
            catch (BrokenRuleException bre)
            {
                response = BadRequest(bre.Message);

            }
            catch (Exception ex)
            {
                // Log the Exception
                await LogHelper.LogErrorAsync(ex);
                response = StatusCode((int)HttpStatusCode.InternalServerError);
            }

            return response;
        }

        [HttpPost]
        [Route("/api/chase/query")]
        [HandleException]
        public async Task<IActionResult> GetChasesQueryListAsync([FromBody]ChaseQuerySearchCriteria model)
        {
            model.CallerUserId = _chaseService.LoginUser.UserId;
            model.UseTransactionalDatabase = true;

            if (model.IsExportAll)
            {
                model.UseTransactionalDatabase = false;
            }

            var chaseQueryListResults = await _chaseService.GetChasesQueryListAsync(model);
            return Ok(chaseQueryListResults);
        }

        /// <summary>
        /// This API is used to get the archived chase query list
        /// </summary>
        /// <returns>Archived Chase Query</returns>
        [HttpPost]
        [Route("/api/chase/query-archive")]
        [HandleException]
        public async Task<IActionResult> GetArchiveChaseQueryListAsync([FromBody] MemberSearchCriteria model)
        {
            model.CallerUserId = _chaseService.LoginUser.UserId;

            var archiveChaseQueryListResults = await _chaseService.GetArchiveChaseQueryListAsync(model);
            return Ok(archiveChaseQueryListResults);
        }

        [HttpGet]
        [Route("/api/chase/query-all")]
        [HandleException]
        public async Task<IActionResult> QueryAllAsync([FromQuery]string search)
        {
            IActionResult response = null;

            var searchModel = new ChaseSearchCriteria
            {
                FullTextSearch = search,
                CallerUserId = _chaseService.LoginUser.UserId,
                UseTransactionalDatabase = false
            };
            var chaseQueryListResults = await _chaseService.ChaseSearchAsync(searchModel);
            chaseQueryListResults.ToList().ForEach(rec =>
            {
                rec.PendCodeAndStatus = !string.IsNullOrEmpty(rec.PendCode) ? $"{rec.PendCode} - {rec.PendStatus}" : "";
            });
            response = Ok(chaseQueryListResults);

            return response;
        }

        [HttpGet]
        [Route("/api/chase/workflowstatuses")]
        [HandleException]
        public async Task<IActionResult> GetWorkflowStatusesAsync()
        {
            IActionResult response = null;

            var chaseWorkflowStatusesResults = await _chaseService.GetWorkflowStatusesAsync();
            response = Ok(chaseWorkflowStatusesResults);

            return response;
        }

        /// <summary>
        /// This API is used to UnAssign Chase.
        /// </summary>
        /// <param name="chaseId"></param>
        /// <returns></returns>
        // Post: api/clinical/assignchase
        [HttpPost]
        [Route("/api/chase/unassignChases")]
        [HandleException]
        public async Task<IActionResult> UnAssignChasesAsync([FromBody]ChaseUnassignModel chaseUnassignModel)
        {
            IActionResult response = null;

            await _chaseService.UnAssignChasesAsync(chaseUnassignModel);
            response = Ok();

            return response;
        }

        [HttpPost]
        [Route("/api/chase/reopen")]
        [HandleException]
        public async Task<IActionResult> ReopenAsync(int chaseId)
        {
            IActionResult response = null;
            var result = await _chaseService.ReopenAsync(chaseId, _chaseService.LoginUser.UserId);

            response = Ok(result);

            return response;
        }

        [HttpGet]
        [Route("/api/chase/reportingstatuses")]
        [HandleException]
        public async Task<IActionResult> GetReportingStatusesAsync(bool bulkOutReach)
        {
            ActionResult response = null;
            IEnumerable<ReportingStatusItem> chaseWorkflowStatusesResults;
            if (bulkOutReach)
            {
                chaseWorkflowStatusesResults = await _chaseService.GetReportingStatusesBulkOutReachAsync();
            }
            else
            {
                chaseWorkflowStatusesResults = await _chaseService.GetReportingStatusesAsync();
            }
             
            response = Ok(chaseWorkflowStatusesResults);

            return response;
        }

        /// <summary>
        /// This API is used to move chases to another AID.
        /// </summary>       
        /// <returns></returns>
        // POST: /api/chase/move 
        [HttpPost]
        [Route("/api/chase/move")]
        [HandleException]
        public async Task<IActionResult> ChaseMoveToAnotherAIDAsync([FromBody]ChaseMoveModel chaseMoveModel)
        {
            IActionResult response = null;

            // TODO: Refactor using LoginInfo
            chaseMoveModel.LoginUserInfo = _chaseService.LoginUser;
            chaseMoveModel.LoginUserId = _chaseService.LoginUser.UserId;
            chaseMoveModel.LoginUserName = _chaseService.LoginUser.UserName;

            var result = await _chaseService.ChaseMoveToAnotherAIDAsync(chaseMoveModel);
            response = Ok(result);

            return response;
        }


        [HttpGet]
        [Route("/api/chase/oktoattachdocument")]
        [HandleException]
        public async Task<IActionResult> OkToAttachDocumentAsync(int chaseId)
        {
            ActionResult response = null;

            var result = await _chaseService.OkToAttachDocumentAsync(chaseId, _chaseService.LoginUser.UserId);

            response = Ok(result);

            return response;
        }


        /// <summary>
        /// This API is used to get chase list recordset based on chase Id.
        /// </summary>
        /// <returns></returns>
        // GET: api/chase/chaseListById
        [HttpGet]
        [Route("/api/chase/chaselistbyid")]
        [HandleException]
        public async Task<IActionResult> GetChaseListByIdAsync(int chaseId)
        {
            ActionResult response = null;

            var result = await _chaseService.GetChaseListByIdAsync(chaseId, _chaseService.LoginUser.UserId);

            response = Ok(result);

            return response;
        }


        /// <summary>
        /// This API is used to get the Member chase query list
        /// </summary>
        /// <returns>Member Chase Query</returns>
        [HttpGet]
        [Route("/api/chase/memberquery")]
        [HandleException]
        public async Task<IActionResult> GetMemberChaseQueryListAsync(int memberId, string dataset,int selectedChase)
        {
            var callerUserId = _chaseService.LoginUser.UserId;

            var archiveChaseQueryListResults = await _chaseService.GetMemberChaseQueryListAsync( memberId, dataset, selectedChase, callerUserId);
            return Ok(archiveChaseQueryListResults);
        }
        /// <summary>
        /// This API is used to Copy Chart to Another Chase.
        /// </summary>
        /// <returns></returns>
        // GET: api/chase/chaseListById
        [HttpPost]
        [Route("/api/chase/copychart")]
        [HandleException]
        public async Task<IActionResult> CopyChartToAnotherChaseAsync(int sourceChaseId, [FromBody] List<int> targetChaseIds)
        {
            ActionResult response = null;

            var result = await _chaseService.CopyChartToAnotherChaseAsync(sourceChaseId, targetChaseIds, _chaseService.LoginUser.UserId);

            response = Ok(result);

            return response;
        }


        /// <summary>Get Chase Data</summary>
        [HttpGet]
        [Route("/api/chase/get/risk/data")]
        [HandleException]
        public async Task<IActionResult> GetRiskChaseDataAsync([FromQuery] int chaseId)
        {
            IActionResult response = null;
            var callerUserId = _chaseService.LoginUser.UserId;
            var chaseData = await _chaseService.GetRiskChaseDataAsync(chaseId, callerUserId);
            response = Ok(chaseData);
            return response;
        }

        /// <summary>Get Chase Data</summary>
        [HttpGet]
        [Route("/api/chase/get/hedis/data")]
        [HandleException]
        public async Task<IActionResult> GetHedisChaseDataAsync([FromQuery] int chaseId)
        {
            IActionResult response = null;
            var callerUserId = _chaseService.LoginUser.UserId;
            var chaseData = await _chaseService.GetHedisChaseDetailAsync(chaseId, callerUserId);
            var chaseDataDto = ChaseDataDto.Ctor(chaseData);
            response = Ok(chaseDataDto);
            return response;
        }
        
        /// <summary>Get Chase Data</summary>
        [HttpGet]
        [Route("/api/chase/get/entities")]
        [HandleException]
        public async Task<IActionResult> GetChaseEntitiesAsync([FromQuery] int chaseId)
        {
            IActionResult response = null;
            var callerUserId = _chaseService.LoginUser.UserId;
            var entities = await _chaseService.GetChaseEntitiesAsync(chaseId, callerUserId);
            response = Ok(entities);
            return response;
        }

        [HttpPost]
        [Route("/api/chase/create")]
        [HandleException]
        public async Task<IActionResult> CreateChaseAsync([FromBody] ChaseCreateRequest chaseCreateRequest)
        {
            IActionResult response = null;
            var callerUserId = _chaseService.LoginUser.UserId;

            var chaseResponse = await _chaseService
                                .CreateChaseAsync(chaseCreateRequest, callerUserId);

            response = Ok(chaseResponse);

            return response;
        }

        [HttpPost]
        [Route("/api/chase/validate")]
        [HandleException]
        public async Task<IActionResult> ValidateNewChaseAsync([FromBody] ChaseCreateRequest chaseCreateRequest)
        {
            IActionResult response = null;
            string result = string.Empty;
            var callerUserId = _chaseService.LoginUser.UserId;

            result = await _chaseService.ValidateNewChaseAsync(chaseCreateRequest, callerUserId);

            response = Ok(result);

            return response;
        }

        [HttpGet]
        [Route("/api/chase/chasekey/unique")]
        [HandleException]
        public async Task<IActionResult> CheckIfChaseKeyIsUniqueAsync(int projectId, string chaseKey)
        {
            IActionResult response = null;
            bool isChaseKeyUnique = false;

            var callerUserId = _chaseService
                                    .LoginUser
                                    .UserId;

            isChaseKeyUnique = await _chaseService
                                        .CheckIfChaseKeyIsUniqueAsync(projectId, chaseKey, callerUserId);

            response = Ok(isChaseKeyUnique);

            return response;
        }

        /// <summary>
        /// Gets list of attribute data related to a measure
        /// </summary>
        /// <param name="measureId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("/api/chase/measure/attribute")]
        [HandleException]
        public async Task<IActionResult> GetAttributeDataByMeasureAsync(int measureId)
        {
            IActionResult response = null;
            IEnumerable<MeasureAttributeData> result = null;

            var callerUserId = _chaseService
                                    .LoginUser
                                    .UserId;

            result = await _chaseService
                                .GetAttributeDataByMeasureAsync(measureId, callerUserId);

            response = Ok(result);

            return response;
        }

        [HttpGet]
        [Route("/api/chase/nlp/data")]
        [HandleException]
        public async Task<IActionResult> GetChaseNlpDataAsync(int chaseId)
        {
            IActionResult response = null;

            var result = await _chaseService
                                    .GetChaseNlpSystemResultsAsync(chaseId);

            response = Ok(result);

            return response;
        }

        [HttpPost]
        [Route("/api/chase/nlp/data/review")]
        [HandleException]
        public async Task SaveReviewedChaseNlpDataAsync([FromBody] ChaseNlpData chaseNlpData)
        {
            await _chaseService
                    .SaveChaseNlpDataAsync(chaseNlpData);
        }


        [HttpGet]
        [Route("/api/chase/validate/assigned")]
        [HandleException]
        public async Task<IActionResult> ValidateChasesForAssignmentAsync(string chaseIds)
        {
            var chaseIdsFromCsv = this.GetChaseIdsList(chaseIds);
            int callerUserId = this._chaseService.LoginUser.UserId;
            var result = await _chaseService.ValidateChasesForAssignmentAsync(chaseIdsFromCsv, callerUserId);
            return Ok(result);
        }

        private List<int> GetChaseIdsList(string chaseIds)
        {
            var chaseIdsFromCsv = chaseIds
                   .Split(',')
                   .Select(a => Convert.ToInt32(a))
                   .ToList();
            return chaseIdsFromCsv;
        }

        [HttpPost]
        [Route("/api/chase/nlp/annotationrequest")]
        [HandleException]
        public async Task<IActionResult> ChaseNlpAnnotationRequestAsync([FromBody] ChaseNlpAnnotationRequest chaseNlpAnnotationRequest)
        {
            await _chaseService.ConvertNLPSupportLocationsToAnnotationAsync(chaseNlpAnnotationRequest);
            return Ok(true);
        }

        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="measureId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("/api/chase/measure/numerators")]
        [HandleException]
        public async Task<IActionResult> GetNumeratorListByMeasureAsync(int projectId, int measureId)
        {
            IActionResult response = null;
            IEnumerable<NumeratorData> result = null;

            var callerUserId = _chaseService
                                    .LoginUser
                                    .UserId;

            result = await _chaseService
                                .GetNumeratorListByMeasureAsync(projectId, measureId, callerUserId);

            response = Ok(result);

            return response;
        }

        /// <summary>
        /// Get risk nlp data including system reults
        /// </summary>
        /// <param name="chaseId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("/api/chase/risknlp/data")]
        [HandleException]
        public async Task<IActionResult> GetRiskNlpDataAsync(int chaseId)
        {
            IActionResult response = null;

            var callerUserId = _chaseService
                                    .LoginUser
                                    .UserId;

            var riskNlpData = await _chaseService
                                        .GetRiskNlpSystemResultsAsync(chaseId, callerUserId);

            response = Ok(riskNlpData);

            return response;
        }

        [HttpPost]
        [Route("/api/chase/risknlp/data/review")]
        [HandleException]
        public async Task SaveReviewedRiskNlpDataAsync([FromBody] RiskNlpData riskNlpData)
        {
            //TODO: Implement the saving of user actions
            //await _chaseService
            //        .SaveRiskNlpDataAsync(riskNlpData);
        }

        /// <summary>
        /// This API will return list of addresses.
        /// </summary>
        [HttpPost]
        [Route("/api/chase/tag/search")]
        public async Task<IActionResult> ChaseTagSearchAsync([FromBody] ChaseSearchCriteria searchCriteria)
        {
            searchCriteria.CallerUserId = _chaseService.LoginUser.UserId;
            var chaseSearchResults = await _chaseService.ChaseTagSearchAsync(searchCriteria);
            IActionResult response = Ok(chaseSearchResults);
            return response;
        }

        /// <summary>
        /// Returns list of NlpHighlights based on chase id and diagnosis information
        /// </summary>
        /// <param name="chaseId"></param>
        /// <param name="encounterId"></param>
        /// <param name="diagnosisId"></param>
        /// <param name="diagnosisCode"></param>
        /// <param name="dosFrom"></param>
        /// <param name="dosThrough"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("/api/chase/diagnosis/nlp/highlights")]
        [HandleException]
        public async Task<IActionResult> GetNlpHighlightsByDiagnosisAsync(int chaseId, int? encounterId, int? diagnosisId, string diagnosisCode, string dosFrom, string dosThrough)
        {
            DocumentPageNlpMatches documentPageNlpMatches = await _chaseService
                                                                        .GetNlpHighlightsByDiagnosisAsync(chaseId, encounterId, diagnosisId, diagnosisCode, dosFrom, dosThrough);

            IActionResult response = Ok(documentPageNlpMatches);

            return response;
        }

        /// <summary>
        /// Returns list of NlpHighlights based on chase id, diagnosis and date of service information
        /// </summary>
        /// <param name="chaseId"></param>
        /// <param name="encounterId"></param>
        /// <param name="diagnosisId"></param>
        /// <param name="diagnosisCode"></param>
        /// <param name="dosFrom"></param>
        /// <param name="dosThrough"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("/api/chase/diagnosis/nlp/dos/highlights")]
        [HandleException]
        public async Task<IActionResult> GetNlpDosHighlightsByDiagnosisAsync(int chaseId, int? encounterId, int? diagnosisId, string diagnosisCode, string dosFrom, string dosThrough)
        {
            DocumentPageNlpMatches documentPageNlpMatches = await _chaseService
                                                                        .GetNlpDosHighlightsByDiagnosisAsync(chaseId, encounterId, diagnosisId, diagnosisCode, dosFrom, dosThrough);

            IActionResult response = Ok(documentPageNlpMatches);

            return response;
        }

        /// <summary>
        /// Returns list of NlpHighlights identified as exclusions based on chase id
        /// </summary>
        /// <param name="chaseId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("/api/chase/diagnosis/nlp/exclusion/highlights")]
        [HandleException]
        public async Task<IActionResult> GetNegativeExclusionHighlightsAsync(int chaseId)
        {
            DocumentPageNlpMatches documentPageNlpMatches = await _chaseService
                                                                       .GetNegativeExclusionHighlightsAsync(chaseId);

            IActionResult response = Ok(documentPageNlpMatches);

            return response;
        }

        /// <summary>
        /// Returns list of NlpHighlights identified as templates based on chase id
        /// </summary>
        /// <param name="chaseId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("/api/chase/diagnosis/nlp/template/highlights")]
        [HandleException]
        public async Task<IActionResult> GetTemplateHighlightsAsync(int chaseId)
        {
            DocumentPageNlpMatches documentPageNlpMatches = await _chaseService
                                                                       .GetTemplateHighlightsAsync(chaseId);

            IActionResult response = Ok(documentPageNlpMatches);

            return response;
        }

        /// <summary>
        /// Returns list of NlpHighlights related to memnber information based on chase id
        /// </summary>
        /// <param name="chaseId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("/api/chase/diagnosis/nlp/member/highlights")]
        [HandleException]
        public async Task<IActionResult> GetMemberHighlightsAsync(int chaseId)
        {
            DocumentPageNlpMatches documentPageNlpMatches = await _chaseService
                                                                       .GetMemberHighlightsAsync(chaseId);

            IActionResult response = Ok(documentPageNlpMatches);

            return response;
        }

        /// <summary>
        /// Saves PrintScreen for Coding Screen
        /// </summary>
        /// <param name="file"></param>
        /// <param name="chaseId"></param>
        /// <param name="auditPackageItemId"></param>
        [HttpPost]
        [Route("/api/chase/savecodingscreenshot")]
        [HandleException]
        public async Task<IActionResult> SaveCodingScreenshotAsync([FromForm] IFormFile file, int chaseId, int auditPackageItemId)
        {
            int callerUserId = this._chaseService.LoginUser.UserId;

            if (file.Length > 0)
            {
                byte[] printScreenBinarydata = null;

                using (var memoryStream = new MemoryStream())
                {
                    await file.CopyToAsync(memoryStream);
                    printScreenBinarydata = memoryStream.ToArray();
                }

                await _chaseService.GeneratePrintScreenDocumentForClinicalModuleAsync(printScreenBinarydata, chaseId, auditPackageItemId, callerUserId);
            }


            return Ok();
        }

        /// <summary>
        /// This API is used to get chase audit log
        /// </summary>
        /// <param name="chaseId"></param>
        /// <returns>Chase Audit Log on chase page</returns>
        [HttpGet]
        [Route("/api/chase/auditlog")]
        [HandleException]
        public async Task<IActionResult> GetChaseAuditLogAsync(int chaseId)
        {
            IActionResult response = null;

            var chaseAudit = await _chaseService.GetChaseAuditLogAsync(chaseId, _chaseService.LoginUser.UserId);
            response = Ok(chaseAudit);

            return response;
        }

        [HttpGet]
        [Route("/api/chase/totalchasepages")]
        [HandleException]
        public async Task<IActionResult> GetTotalMemberChasePagesAsync([FromQuery] int chaseId)
        {
            var data = await _chaseService.GetTotalMemberChasePagesAsync(chaseId);

            return Ok(data);
        }
    }
}