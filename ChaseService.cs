using HDVI.Core.AWS.ServiceContract;
using HDVI.Core.MRCS.Common;
using HDVI.Core.MRCS.DataContract;
using HDVI.Core.MRCS.DataContract.Audit;
using HDVI.Core.MRCS.DataContract.Catalytic;
using HDVI.Core.MRCS.DataContract.Chase;
using HDVI.Core.MRCS.DataContract.Chase.ChaseData;
using HDVI.Core.MRCS.DataContract.Clinical;
using HDVI.Core.MRCS.DataContract.CommentItem;
using HDVI.Core.MRCS.DataContract.Database;
using HDVI.Core.MRCS.DataContract.Document;
using HDVI.Core.MRCS.DataContract.Entity;
using HDVI.Core.MRCS.DataContract.Enums;
using HDVI.Core.MRCS.DataContract.EzDI;
using HDVI.Core.MRCS.DataContract.Member;
using HDVI.Core.MRCS.DataContract.OCR;
using HDVI.Core.MRCS.DataContract.Pend;
using HDVI.Core.MRCS.DataContract.Reporting;
using HDVI.Core.MRCS.DataContract.Tags;
using HDVI.Core.MRCS.DataContract.Workflow;
using HDVI.Core.MRCS.RepositoryContract.Chase;
using HDVI.Core.MRCS.RepositoryContract.Member;
using HDVI.Core.MRCS.ServiceContract.Alert;
using HDVI.Core.MRCS.ServiceContract.Audit;
using HDVI.Core.MRCS.ServiceContract.Chase;
using HDVI.Core.MRCS.ServiceContract.Document;
using HDVI.Core.MRCS.ServiceContract.Entity;
using HDVI.Core.MRCS.ServiceContract.OCR;
using HDVI.Core.MRCS.ServiceContract.Pend;
using HDVI.Core.MRCS.ServiceContract.ProjectConfiguration;
using HDVI.Core.MRCS.ServiceContract.Tags;
using HDVI.Core.MRCS.ServiceContract.User;
using IronPdf;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Formatting = Newtonsoft.Json.Formatting;

namespace HDVI.Core.MRCS.Service.Chase
{
    /// <summary>
    /// Service to work with Chase related services.
    /// </summary>
    public class ChaseService : BaseService, IChaseService
    {
        private readonly IChaseRepository _chaseRepository;
        private readonly IMemberRepository _memberRepository;
        private readonly IUserService _userService;
        private readonly IAlertService _alertService;
        private readonly IProjectConfigurationService _projectConfigurationService;
        private readonly IPendService _pendService;
        private readonly IS3Service _s3Service;
        private readonly IOCRService _ocrService;
        private readonly IChaseDocumentService _chaseDocumentService;
        private readonly IChaseDocumentRepository _chaseDocumentRepository;
        private readonly IAuditService _auditService;
        private readonly IAnnotationService _annotationService;
        private readonly ITagsService _tagsService;
        private readonly IEntityService _entityService;
        private IConfiguration _configuration;

        JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings { ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore };

        public ChaseService(
            IChaseRepository chaseRepository,
            IMemberRepository memberRepository,
            IChaseDocumentRepository chaseDocumentsRepository,
            IUserService userService,
            IProjectConfigurationService projectConfigurationService,
            IPendService pendService,
            IConfiguration configuration,
            IAlertService alertService,
            IS3Service s3Service,
            IOCRService ocrService,
            IChaseDocumentService chaseDocumentService,
            IAuditService auditService,
            IAnnotationService annotationService,
            ITagsService tagsService,
            IEntityService entityService
            ) : base(chaseRepository, configuration)
        {
            _chaseRepository = chaseRepository;
            _memberRepository = memberRepository;
            _chaseDocumentRepository = chaseDocumentsRepository;
            _userService = userService;
            _alertService = alertService;

            _projectConfigurationService = projectConfigurationService;
            _pendService = pendService;
            _s3Service = s3Service;
            _ocrService = ocrService;
            _chaseDocumentService = chaseDocumentService;
            _auditService = auditService;
            _annotationService = annotationService;
            _tagsService = tagsService;
            _entityService = entityService;
            _configuration = configuration;
        }

        /// <summary>
        /// ChaseService is overriding the BaseService's Healthcheck and calling its own Repo's Healthcheck following its own Service-to-Repo tier line.
        /// </summary>
        /// <returns>HealthCheck Information</returns>
        [Obsolete]
        public override async Task<object> HealthCheckAsync()
        {
            return await _chaseRepository.HealthCheckAsync();
        }

        /// <summary>
        /// Gets the Metadata for the ChaseDetail and Timeline component in the Chase Detail Page.
        /// </summary>
        /// <returns>Metadata for the ChaseDetail and Timeline component</returns>
        public IEnumerable<ChaseDetailMetadata> GetChaseSummaryTimelineMetadata()
        {
            var chaseDetailMetadataList = new List<ChaseDetailMetadata>()
            {
                new ChaseDetailMetadata{
                    ComponentId = "chasedetail_summary",
                    Title = " ",
                    Readonly = true,
                    ApiUrl = "api/Chase/{chaseId}/Summary"
                },
                new ChaseDetailMetadata{
                    ComponentId = "chasedetail_timeline",
                    Title = "CHASE TIMELINE ",
                    Readonly = true,
                    ApiUrl = "api/Chase/{chaseId}/TimelineDetails"
                },
            };

            return chaseDetailMetadataList;
        }

        public async Task<bool> IsValidChaseId(int chaseId, int callerUserId)
        {
            bool retValue = false;

            try
            {
                ChaseDetailSummary chaseDetail = await _chaseRepository.GetChaseDetailByIdAsync(chaseId, null, null, callerUserId);
                if (chaseDetail != null)
                    retValue = true;

            }
            catch (SqlException sqlEx)
            {
                if (sqlEx != null)
                {
                    // If Invalid ChaseId error message, catch errors and return false
                    if (sqlEx.Number == 50000)
                        retValue = false;
                    else throw;
                }
            }

            return retValue;
        }

        // <summary>
        /// Get Chase Detail Summary
        /// </summary>
        /// <param name="chaseId"></param>
        /// <param name="callerUserId"></param>
        /// <returns>ChaseDetailSummary</returns>
        public async Task<ChaseDetailSummary> GetChaseDetailSummary(int chaseId, int callerUserId)
        {
            ChaseDetailSummary chaseDetail = await _chaseRepository.GetChaseDetailByIdAsync(chaseId, null, null, callerUserId);
            return chaseDetail;
        }

        /// <summary>
        /// Service to get the chase details summary.
        /// </summary>
        /// <param name="chaseId"></param>
        /// <param name="projectId"></param>
        /// <param name="workflowStatus"></param>
        /// <param name="callerUserId"></param>
        /// <returns>Chase Details</returns>
        public async Task<ChaseDetailSummary> GetChaseDetailByIdAsync(int chaseId, int? projectId, string workflowStatus, int callerUserId)
        {
            return await _chaseRepository.GetChaseDetailByIdAsync(chaseId, null, null, callerUserId);
        }

        /// <summary>
        /// Service to get the chase details summary.
        /// </summary>
        /// <param name="chaseId"></param>
        /// <returns>Chase details</returns>
        public async Task<IEnumerable<KeyValueItem>> GetChaseSummaryAsync(int chaseId, int userId)
        {
            ChaseDetailSummary chaseDetail = await GetChaseDetailByIdAsync(chaseId, null, null, userId);
            var assignedToUser = this.GetAssignedTo(chaseDetail.ChaseTeamData);
            var chaseSummary = new List<KeyValueItem>()
            {
                new KeyValueItem
                {
                    Key = "Measure ID",
                    Value = chaseDetail.MeasureCode
                },
                new KeyValueItem
                {
                    Key = "Status",
                    Value = chaseDetail.ReportingStatusName
                },
                new KeyValueItem
                {
                    Key = "Last Coder",
                    Value = chaseDetail.LastCoder,
                    Url = "api/User/CoderID"
                },
                new KeyValueItem
                {
                    Key = "Project",
                    Value = chaseDetail.ProjectName
                },
                new KeyValueItem
                {
                    Key = "ProjectId",
                    Value = Convert.ToString(chaseDetail.ProjectID)
                },
                new KeyValueItem
                {
                    Key = "Client",
                    Value = chaseDetail.ClientName
                },
                new KeyValueItem
                {
                    Key = "Product",
                    Value = chaseDetail.Product
                },
                new KeyValueItem
                {
                    Key = "Assigned To",
                    Value = assignedToUser?.FullName,
                },
                new KeyValueItem
                {
                    Key = "Member ID",
                    Value = Convert.ToString(chaseDetail.MemberID)
                },
                new KeyValueItem
                {
                    Key = "Client Chase Key",
                    Value = chaseDetail.ChaseSourceAliasID,
                },
                new KeyValueItem
                {
                    Key = "AID",
                    Value = Convert.ToString(chaseDetail.MasterDocumentSourceID),
                    Url = "/retrieval/address/" +  chaseDetail.MasterDocumentSourceID + "/" + chaseDetail.DocumentSourceTypeID
                },
                new KeyValueItem
                {
                    Key = "Pend Code",
                    Value = chaseDetail.PendCode
                },
                new KeyValueItem
                {
                    Key = "Sex",
                    Value = chaseDetail.MemberGender
                },
                new KeyValueItem
                {
                    Key = "DOB",
                    Value = chaseDetail.MemberDateOfBirth?.ToShortDateString()
                },
                new KeyValueItem
                {
                    Key = "ChasePendId",
                    Value = chaseDetail.ChasePendId?.ToString(),
                    Url = "Pend/detail/" +  chaseDetail.ChasePendId
                },
                new KeyValueItem
                {
                    Key = "PendCodeName",
                    Value = chaseDetail.PendCodeName
                },
                new KeyValueItem
                {
                    Key = "Owner",
                    Value = chaseDetail.Owner
                },
                new KeyValueItem
                {
                    Key = "Member Name",
                    Value = $"{chaseDetail.MemberFirstName} {chaseDetail.MemberLastName}",
                },
                new KeyValueItem
                {
                    Key = "MeasureYear",
                    Value = chaseDetail.MeasureYear.ToString(),
                },
                new KeyValueItem
                {
                    Key = "Assigned To Id",
                    Value = assignedToUser?.UserId.ToString(),
                },
                new KeyValueItem
                {
                    Key = "Age",
                    Value = chaseDetail.MemberAge.ToString()
                },
                new KeyValueItem
                {
                    Key = "Address ID",
                    Value = Convert.ToString(chaseDetail.AddressID),
                    Url = "api/Site/" +  chaseDetail.AddressID
                },
                new KeyValueItem
                {
                    Key = "Workflow Status",
                    Value = chaseDetail.WorkflowStatusName
                },
                new KeyValueItem
                {
                    Key = "Project Type Id",
                    Value = chaseDetail.ProjectTypeId.ToString(),
                },
                new KeyValueItem
                {
                    Key = "Enrollee ID",
                    Value = chaseDetail.MemberEnrolleeId
                },
                new KeyValueItem
                {
                    Key = "Client Member ID",
                    Value = chaseDetail.MemberSourceAliasID
                },
                new KeyValueItem
                {
                    Key = "PendStatusId",
                    Value = chaseDetail.PendStatusId.ToString(),
                },
                new KeyValueItem
                {
                    Key = "PendStatusName",
                    Value = chaseDetail.PendStatusName,
                },
                new KeyValueItem
                {
                    Key = "Project Date Range",
                    Value = chaseDetail.ProjectDuration,
                },
                new KeyValueItem
                {
                    Key = "ChaseSubmissionItem",
                    Value = chaseDetail.ChaseSubmissionItem,
                },
                new KeyValueItem
                {
                    Key = "MemberValidationReason",
                    Value = chaseDetail.MemberValidationReason
                },
                new KeyValueItem
                {
                    Key = "ChaseDocumentAnnotation",
                    Value = chaseDetail.ChaseDocumentAnnotation.ToString()
                },
                new KeyValueItem
                {
                    Key = "UserDefinedValue",
                    Value = chaseDetail.UserDefinedValue
                }
            };

            return chaseSummary;
        }

        private ChaseTeam GetAssignedTo(string chaseTeamData)
        {
            if (String.IsNullOrWhiteSpace(chaseTeamData))
            {
                return null;
            }

            ChaseTeam assignedTo = null;
            var chaseTeams = XmlHelper.GetFromXml<ChaseTeams>(chaseTeamData);
            if (chaseTeams != null && chaseTeams.Teams != null && chaseTeams.Teams.Any())
            {
                assignedTo = chaseTeams.Teams.FirstOrDefault(x => x.CurrentTaskAssignment == 1);
            }

            return assignedTo;
        }


        /// <summary>
        /// Returns risk chase data based on chase id.
        /// </summary>
        /// <param name="chaseId"></param>
        /// <param name="callerUserId"></param>
        /// <param name="organizationId"></param>
        /// <returns></returns>
        public async Task<ChaseDetail> GetChaseDetailAsync(int chaseId, int callerUserId, int? organizationId = null)
        {
            ChaseDetail chaseResult = new ChaseDetail();
            chaseResult = await _chaseRepository.GetChaseDetailAsync(chaseId, callerUserId);
            if (chaseResult != null)
            {
                await SetDisplayNlpResultsAsync(chaseResult);

                chaseResult.OcrDataAvailable = await OcrDataAvailableAsync(chaseResult.ChaseId);

                if(organizationId != null)
                {
                    chaseResult.DisplayOcrBlocks = IsOrganizationEnabledForOcrBlockDisplay(organizationId.Value);
                }
                
                var assignedToUser = this.GetAssignedTo(chaseResult.ChaseTeamData);
                if (assignedToUser != null)
                {
                    chaseResult.AssignedToName = assignedToUser.FullName;
                }

                if (chaseResult.IsMemberCentric)
                {
                    chaseResult.MemberCentricChases = await _memberRepository.GetMemberCentricChaseDataAsync(chaseId, "MemberChase");
                }
            }
            return chaseResult;
        }

        /// <summary>
        /// Returns Archived Chase Detail
        /// </summary>
        /// <param name="chaseId"></param>
        /// <param name="callerUserId"></param>
        /// <returns></returns>
        public async Task<ChaseArchive> GetChaseArchiveAsync(int chaseId, int callerUserId)
        {
            ChaseArchive chaseArchive = new ChaseArchive();
            chaseArchive = await _chaseRepository.GetChaseArchiveAsync(chaseId, callerUserId);

            return chaseArchive;
        }

        /// <summary>
        /// Check if Documents for this Chase have OCR Data available
        /// </summary>
        /// <param name="chaseId"></param>
        /// <returns></returns>
        public async Task<bool> OcrDataAvailableAsync(int chaseId)
        {
            bool ocrDataAvailable = false;

            try
            {
                if (chaseId > 0)
                {
                    // need to check every document this chase is made from
                    var chaseDocuments = await _chaseDocumentService.GetChaseDocumentsAsync(chaseId, 0, (int)DocumentType.MedicalRecord, false);
                    if (chaseDocuments != null)
                    {
                        foreach (var chaseDocument in chaseDocuments)
                        {
                            // Only have to find OCR data for one document to be true
                            if (_ocrService.OCRDataAvailableAsync(Path.GetFileNameWithoutExtension(chaseDocument.DocumentQueueStorageFileKey)).Result)
                            {
                                ocrDataAvailable = true;
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError("SetOcrDataAvailableAsync", ex);
                // dont throw here
            };

            return ocrDataAvailable;

        }

        /// <summary>
        /// Set the display nlp result property based on nlp process results
        /// </summary>
        /// <param name="chaseResult"></param>
        /// <returns></returns>
        private async Task SetDisplayNlpResultsAsync(ChaseDetail chaseResult)
        {
            if (chaseResult != null)
            {
                if ((int)chaseResult.ProjectTypeId == (int)ProjectTypeEnum.HEDIS)
                {
                    if (IsOrganizationEnabledForHedisNlp(chaseResult.OrganizationId))
                    {
                        // try to get the nlp data
                        var chaseNlpData = await GetChaseNlpSystemResultsAsync(chaseResult.ChaseId);

                        // if no data avaiable or user has not completed revieweing the system results,
                        // set the display result to false
                        chaseResult.DisplayNlpResults = ((chaseNlpData != null) && !chaseNlpData.SystemResultsReviewed);
                    }
                }
            }
        }

        /// <summary>
        /// Returns chase data based on chaseid.
        /// </summary>
        /// <param name="chaseId"></param>
        /// <param name="callerUserId"></param>
        /// <param name="workflowStatus"></param>
        [Obsolete("Use GetHedisChaseDetailAsync instead.")]
        public async Task<HedisChaseData> GetChaseDataByIdAsync(int chaseId, int callerUserId, string workflowStatus = null)
        {
            var result = await _chaseRepository.GetHedisChaseDetailAsync(chaseId, callerUserId, workflowStatus);

            return result;
        }

        /// <summary>
        /// Returns risk chase data based on chase id.
        /// </summary>
        /// <param name="chaseId"></param>
        /// <param name="callerUserId"></param>
        /// <param name="workflowStatus"></param>
        public async Task<RiskChaseData> GetRiskChaseDetailAsync(int chaseId, int callerUserId, string workflowStatus = null)
        {
            var result = await _chaseRepository
                                    .GetRiskChaseDetailAsync(chaseId, callerUserId, workflowStatus);

            return result;
        }

        /// <summary>
        /// Returns hedis chase data based on chase id.
        /// </summary>
        /// <param name="chaseId"></param>
        /// <param name="callerUserId"></param>
        /// <param name="workflowStatus"></param>
        public async Task<HedisChaseData> GetHedisChaseDetailAsync(int chaseId, int callerUserId, string workflowStatus = null)
        {
            var result = await _chaseRepository
                                .GetHedisChaseDetailAsync(chaseId, callerUserId, workflowStatus);

            return result;
        }

        /// <summary>
        /// Returns Chart page numbers used for data entry for all member chases.
        /// </summary>
        /// <param name="chaseId"></param>
        /// <param name="callerUserId"></param>
        public async Task<IEnumerable<int>> GetMemberChasesDataEntryPagesAsync(int chaseId, int callerUserId)
        {
            List<int> allDataEntryPages = new List<int>();
            var memberChases = await _memberRepository.GetMemberCentricChaseDataAsync(chaseId, "MemberChase");

            memberChases.Select(m => m.ChaseId).ToList().ForEach(memberChaseId =>
            {
                var result = _chaseRepository
                                .GetHedisChaseDetailAsync(memberChaseId, callerUserId).Result;

                var dateEntryPageForChase = result.ChaseData
                                                  .Where(c => c.EntityTypeName != "Miscellaneous" && c.EntityTypeName != "Workflow")
                                                  .SelectMany(x => x.Attributes)
                                                  .Any(x => x.AttributeCode == "ChartPageNumber")
                                                 ? result.ChaseData
                                                   .Where(c => c.EntityTypeName != "Miscellaneous" && c.EntityTypeName != "Workflow")
                                                   .SelectMany(x => x.Attributes)
                                                   .Where(x => x.AttributeCode == "ChartPageNumber")
                                                   .Select(x => Convert.ToInt32(x.Value))
                                                 :  new List<int>();

                allDataEntryPages.AddRange(dateEntryPageForChase);

            });

            return allDataEntryPages.Distinct();

        }

        /// <summary>
        /// Returns hedis chase data based on chase id.
        /// </summary>
        /// <param name="chaseId"></param>
        /// <param name="callerUserId"></param>
        /// <param name="workflowStatus"></param>
        public async Task<IEnumerable<DynamicEntity>> GetChaseEntitiesAsync(int chaseId, int callerUserId, string workflowStatus = null)
        {
            var genericChase = await _chaseRepository.GetChaseDetailGenericAsync(chaseId, callerUserId, workflowStatus);
            var entities = GetChaseData(genericChase);
            return entities;
        }

        private List<DynamicEntity> GetChaseData(spMR50_ChaseDetail_sel chaseDetail)
        {
            var result = new List<DynamicEntity>();

            if (XmlHelper.TryGetFromXml<HedisEntities>(chaseDetail.EntityData, out var data))
            {
                result = data.Entities.Select(a =>
                {
                    var newEntity = new DynamicEntity(a.Attributes);
                    newEntity.SetIdsAndApplyToAttributes(chaseDetail.ChaseId, a.EntityId, null, a.EntityTypeId);
                    return newEntity;
                }).ToList();
            }

            return result;
        }

        /// <summary>
        /// Service to get chase comments.
        /// </summary>
        /// <param name="chaseId"></param>
        /// <param name="isOnlyLatest"></param>
        /// <returns>Collection of Comments to api</returns>
        public async Task<IEnumerable<CommentItem>> GetChaseCommentsAsync(int chaseId, bool isOnlyLatest)
        {
            var chaseComments = await _chaseRepository.GetChaseCommentsAsync(chaseId);

            if (chaseComments == null || !chaseComments.Any())
            {
                return new List<CommentItem>();
            }
            return isOnlyLatest
                    ? new List<CommentItem>() { chaseComments.ToList().FirstOrDefault() }
                    : chaseComments;
        }

        /// <summary>
        /// Service to get chase & pend comments.
        /// </summary>
        /// <param name="chaseId"></param>
        /// <param name="userId"></param>
        /// <returns>Collection of Comments to api</returns>
        public async Task<IEnumerable<CommentItem>> GetChasePendCommentsAsync(int chaseId, int userId)
        {
            List<CommentItem> chasePendComments = new List<CommentItem>();
            var chaseComments = await _chaseRepository.GetChaseCommentsAsync(chaseId);

            if (chaseComments.Count() > 0)
                chasePendComments.AddRange(chaseComments);

            var pendComments = await GetPendCommentsAsync(chaseId, userId);

            if (pendComments != null && pendComments.Any())
                chasePendComments.AddRange(pendComments);

            if (chasePendComments.Count() > 0)
                return chasePendComments.OrderByDescending(x => x.CreatedDate);
            else
                return chasePendComments;
        }

        private async Task<IEnumerable<CommentItem>> GetPendCommentsAsync(int chaseId, int userId)
        {
            IEnumerable<CommentItem> pendComments = null;
            _pendService.LoginUser = this.LoginUser;

            var pendItems = await _pendService.GetPendListAsync(new PendSearchCriteria { ChaseId = chaseId, CallerUserId = userId });

            if (pendItems.Count() > 0)
            {
                var chasePendIds = pendItems.Select(x => x.ChasePendId.Value).ToList();

                pendComments = await _pendService.GetAllPendsCommentsAsync(chasePendIds);

                pendComments = pendComments.Where(x => !string.IsNullOrEmpty(x.CommentText));
                pendComments.ToList().ForEach(x => x.CommentType = "From Pend Comments");
            }

            return pendComments;
        }

        /// <summary>
        /// Service to save chase comments.
        /// </summary>
        /// <param name="chaseId"></param>
        /// <param name="commentText"></param>
        public async Task AddChaseCommentAsync(int chaseId, string commentText, int userId)
        {
            if (!String.IsNullOrWhiteSpace(commentText))
            {
                await _chaseRepository.AddChaseCommentAsync(new CommentItem
                {
                    ObjectId = chaseId,
                    CommentTypeId = (int)CommentType.Chase,
                    CommentText = commentText,
                    CreatedByUserId = userId,
                });
            }
        }

        /// <summary>
        /// Returns a list of Chase data based on values in chaseSearchCriteria
        /// </summary>
        /// <param name="ChaseSearchCriteria chaseSearchCriteria"></param>
        /// <returns></returns>
        public async Task<IEnumerable<ChaseSearchResult>> ChaseSearchAsync(ChaseSearchCriteria chaseSearchCriteria)
        {
            var records = await _chaseRepository.ChaseSearchAsync(chaseSearchCriteria);
            if (records != null)
            {
                var chaseList = records.ToList();
                //TODO: This will be a part of next release
                // Commenting due to performance issue. Why do we need to call tag sel for each chase?
                //await GetTags(chaseList);

                chaseList.ForEach(rec =>
                {
                    if (rec != null && !String.IsNullOrEmpty(rec.ServiceProviderData))
                    {
                        var xml = rec.ServiceProviderData;
                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.LoadXml(xml);
                        var nodes = xmlDoc.SelectNodes("//serviceproviders/serviceprovider/name");
                        int nodecount = nodes.Count;
                        if (nodes != null)
                        {
                            StringBuilder sb = new StringBuilder();
                            foreach (XmlNode node in nodes)
                            {
                                sb.AppendFormat("{0},", node.InnerText);

                            }
                            rec.ServiceProviderData = sb.ToString().TrimEnd(',');
                        }

                        rec.ProviderCount = nodecount;
                    }
                });

                return chaseList;
            }
            return null;
        }

        private async Task<IEnumerable<ChaseSearchResult>> GetTags(IEnumerable<ChaseSearchResult> chaseList)
        {
            int callerUserId = this.LoginUser.UserId;
            int organisationId = this.LoginUser.OrganizationId;
            foreach (var chase in chaseList)
            {
                chase.tagItems = await _tagsService.GetTagsItemAsync(organisationId, chase.ChaseID, TagType.Chase, callerUserId);
                if (chase.tagItems.Count() > 0)
                {
                    chase.Tags = String.Join(",", chase.tagItems.Select(p => p.TagText));
                }

            }
            return chaseList;
        }
        /// <summary>
        /// Get a chase query list.
        /// </summary>
        public async Task<IEnumerable<ChaseQueryItem>> GetChasesQueryListAsync(ChaseQuerySearchCriteria model)
        {
            int callerUserId = this.LoginUser.UserId;
            int organisationId = this.LoginUser.OrganizationId;
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (model.CallerUserId < 1)
            {
                throw new ArgumentException("Missing caller user id.", nameof(model.CallerUserId));
            }

            if (String.IsNullOrWhiteSpace(model.StatisticsFilter))
            {
                model.StatisticsFilter = null;
            }

            if (model.AssignedToUserId == 0)
            {
                model.AssignedToFilter = "Unassigned";
                model.AssignedToUserId = null;
            }
            else
            {
                model.AssignedToFilter = null;
            }

            if (model.DateAssigned != null)
            {
                model.DateAssigned = DateTime.ParseExact(model.DateAssigned, "MM/dd/yyyy", null).ToString();
            }

            model.convertProjectsCsvToXml();
            model.convertStatusesCsvToXml();
            model.convertMeasuresCodesCsvToXml();
            SetHccDiscrepencyAndEncounterFoundValue(model);

            var result = await _chaseRepository.GetChasesQueryListAsync(model);
            var chaseList = result.ToList();
            //TODO: This will be a part of next release
            //foreach (var chase in chaseList)
            //{
            //    chase.tagItems = await _tagsService.GetTagsItemAsync(organisationId, chase.ChaseId, callerUserId);
            //}
            return chaseList;
        }

        /// <summary>
        /// Returns Archived Chase Query List
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<ChaseQueryItem>> GetArchiveChaseQueryListAsync(MemberSearchCriteria model)
        {
            if (model == null || model.CallerUserId < 1)
            {
                IEnumerable<ChaseQueryItem> empty = Enumerable.Empty<ChaseQueryItem>();
                return empty;
            }

            var result = await _chaseRepository.GetArchiveChaseQueryListAsync(model);
            if (result !=null) {
                result.ToList().ForEach(x =>
               {

                   var attributeId = 265;
                   var attribute = _projectConfigurationService.GetProjectAttributeAsync(x.ProjectID, attributeId, model.CallerUserId);
                   if (attribute != null && attribute.Result.AttributeValue != "1" || x.DataSet == "Archived")
                   {
                       x.isChaseNonMemberCentric = checkChaseHasChartAssigned(x);
                       x.Disabled = (x.DataSet == "Archived" || x.DataSet == "Inactive");
                   }
                   x.DocumentSourceLocation = x.DocumentSourceCity + ", " + x.DocumentSourceState;
               });
            }

            return result;
        }

        private bool checkChaseHasChartAssigned(ChaseQueryItem chase)
        {
            bool isChaseNonMemberCentric = false;
            bool checkChaseStatus = chase.WorkflowStatusName != "Chart collection" && chase.WorkflowStatusName != "Waiting for chart";
            bool checkPendCode = chase.PendCode == "PC900";
            bool checkPendStatus = chase.PendStatusId == (int)DataContract.Enums.PendStatus.New
                                    || chase.PendStatusId == (int)DataContract.Enums.PendStatus.InProgress
                                    || chase.PendStatusId == (int)DataContract.Enums.PendStatus.Closed;

            if (checkChaseStatus || (checkPendCode && checkPendStatus))
            {
                isChaseNonMemberCentric = true;
            }

            return isChaseNonMemberCentric;
        }

        /// <summary>
        /// Return a list of chase WorkflowStatus.
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<WorkflowStatusItem>> GetWorkflowStatusesAsync()
        {
            return await _chaseRepository.GetWorkflowStatusesAsync();
        }

        /// <summary>
        /// Return a list of chase ReportingStatusBulkOutReach.
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<ReportingStatusItem>> GetReportingStatusesBulkOutReachAsync()
        {
            var list = new List<string>() { "Pended", "Unscheduled", "Past Due", "Scheduled" };
            List<ReportingStatusItem> setStatus = new List<ReportingStatusItem>();
            var statusList = await _chaseRepository.GetReportingStatusesAsync();
            if (statusList != null)
            {
                foreach (var status in statusList)
                {
                    if (list.Contains(status.ReportingStatusName))
                    {
                        ReportingStatusItem setValue = new ReportingStatusItem();
                        setValue.ReportingStatusName = status.ReportingStatusName;
                        setStatus.Add(setValue);
                    }
                }
            }

            return setStatus;
        }

        /// <summary>
        /// Return a list of chase ReportingStatus.
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<ReportingStatusItem>> GetReportingStatusesAsync()
        {
            return await _chaseRepository.GetReportingStatusesAsync();
        }

        /// <summary>
        /// Service to unassign Chases.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> UnAssignChasesAsync(ChaseUnassignModel unassignModel)
        {

            unassignModel.CallerUserId = unassignModel.UserId = LoginUser.UserId;
            unassignModel.ChaseListXML = new DataContract.Clinical.ChaseItems
            {
                Chase = unassignModel.ChaseList
                                     .Select(id => new DataContract.Clinical.ChaseItem
                                     {
                                         ChaseId = id
                                     }
                                     ).ToList()
            };

            return await _chaseRepository.UnAssignChasesAsync(unassignModel);
        }

        /// <summary>
        /// Reopens chase in Closed Status
        /// </summary>
        /// <param name="chaseId"></param>
        /// <param name="chaseId"></param>
        /// <param name="callerUserId"></param>
        /// <returns></returns>
        public async Task<bool> ReopenAsync(int chaseId, int callerUserId)
        {
            return await _chaseRepository.ReopenAsync(chaseId, callerUserId);
        }

        /// <summary>
        /// Chase move to another address id.
        /// </summary>        
        /// <returns></returns>
        public async Task<bool> ChaseMoveToAnotherAIDAsync(ChaseMoveModel model)
        {
            // Convert Chases to XML.
            var chaseIdsXML = string.Empty;
            var chaseIdsList = model.ChaseIds.Split(',');
            if (!string.IsNullOrEmpty(model.ChaseIds))
            {
                var chasesIds = new ChaseItems();
                var chase = new List<DataContract.Clinical.ChaseItem>();
                var chaseIds = model.ChaseIds.Split(',');

                foreach (var item in chaseIds)
                {
                    chase.Add(new DataContract.Clinical.ChaseItem()
                    {
                        ChaseId = Convert.ToInt32(item)
                    });
                }

                chasesIds.Chase = chase;
                chaseIdsXML = XmlHelper.GetAsXml(chasesIds);
            }
            model.ChaseIds = chaseIdsXML;

            // Chase is moved directly when an admin/lead/manager performs a chase move.
            // Else, a move request is queued in approval center, to be approved or denied later by the auhorized user.
            // Status is "Pending Approval" for non-manger/lead/admin users.
            var response = await _chaseRepository.ChaseMoveToAnotherAIDAsync(model);

            if (response.Equals(true))
            {
                // Trigger Timeline events immediately only if user is admin/lead/manager. For other users trigger timeline events
                // after the move requests approved/denied from approval center.
                if (!string.Equals(model.Status, "Pending Approval", StringComparison.OrdinalIgnoreCase))
                {
                    // added timeline for destination address
                    await _chaseRepository.AddChaseCommentAsync(new CommentItem
                    {
                        ObjectId = Convert.ToInt32(model.ToAddressId),
                        CommentTypeId = (int)CommentType.DocumentSource,
                        CommentText = string.Format(@"{0} Chases moved from location {1} to location {2}.", chaseIdsList.Count(), model.FromAddressId, model.ToAddressId),
                        CreatedByUserId = model.LoginUserId,
                    });

                    // added timeline for source address
                    await _chaseRepository.AddChaseCommentAsync(new CommentItem
                    {
                        ObjectId = Convert.ToInt32(model.FromAddressId),
                        CommentTypeId = (int)CommentType.DocumentSource,
                        CommentText = string.Format(@"{0} Chases moved from location {1} to location {2}.", chaseIdsList.Count(), model.FromAddressId, model.ToAddressId),
                        CreatedByUserId = model.LoginUserId,
                    });
                }
            }
            else
            {
                response = false;
            }

            return response;
        }

        /// <summary>
        /// Determine if Chase is in proper status  for attaching another document 
        /// </summary>
        /// <param name="chaseId"></param>
        /// <param name="callerUserId"></param>
        /// <returns></returns>
        public async Task<OkToAttachDocumentResponse> OkToAttachDocumentAsync(int chaseId, int callerUserId)
        {
            OkToAttachDocumentResponse retValue = OkToAttachDocumentResponse.Denied;

            ChaseDetailSummary chase = await _chaseRepository.GetChaseDetailByIdAsync(chaseId, null, null, callerUserId);
            if (chase != null)
            {

                // if Chase chart attached allowed
                if (this.IsWorkflowStatusForChartAttach(chase.WorkflowStatusId))

                    retValue = OkToAttachDocumentResponse.OkToAttach;
                else
                {
                    // Check project config, if Approval turned on then set to approval needed,otherwise not allowed if >= MRQA
                    _projectConfigurationService.LoginUser = LoginUser;

                    var attributeList = await _projectConfigurationService.GetProjectConfigurationAttributeAsync(chase.ProjectID, 201);
                    if (attributeList?.Count() > 0)
                    {
                        bool allowDocumentForInProcessChase = attributeList.First().AttributeValue == "1" ? true : false;
                        if (allowDocumentForInProcessChase)
                        {
                            retValue = OkToAttachDocumentResponse.NeedsApproval;
                        }
                    }
                }
            }

            return retValue;
        }

        /// <summary>
        /// Service is used to get chase list recordset based on chase Id.
        /// </summary>
        /// <param name="chaseId"></param>
        /// <param name="callerUserId"></param>
        /// <returns>IEnumerable<ChaseSearchResult></returns>
        public async Task<IEnumerable<ChaseSearchResult>> GetChaseListByIdAsync(int chaseId, int callerUserId)
        {
            return await _chaseRepository.GetChaseListByIdAsync(chaseId, callerUserId);
        }

        /// <summary>
        /// This service is used to get chases for Member list recordset based on Member Id.
        ///  <param name="memberId"></param>
        /// <param name="dataSet"></param>
        /// <param name="callerUserId"></param>
        /// <returns>IEnumerable<ChaseSearchResult></returns>
        /// </summary>
        public async Task<IEnumerable<ChaseSearchResult>> GetMemberChaseQueryListAsync(int memberId, string dataSet, int selectedChaseId, int callerUserId)
        {
            var result = await _chaseRepository.GetMemberChaseQueryListAsync(memberId, dataSet,callerUserId);
            if (result != null)
            {
                result.ToList().ForEach(x =>
                {
                    x.Disabled = checkIfChartAssignToMemeberChase(x, selectedChaseId);
                });
                result = result != null ? result.Where(x => !x.Disabled) : null; 
            }
          
            return result;
        }
        private bool checkIfChartAssignToMemeberChase(ChaseSearchResult chase,int selectedChaseId)
        {
            bool isDisabled = false;

            bool checkChaseDataSet = chase.DataSet == "Archived";
            bool checkChaseStatus = chase.WorkflowStatusName != "Chart collection" && chase.WorkflowStatusName != "Waiting for chart";
            bool checkSelectedChase = chase.ChaseID == selectedChaseId;
            bool checkPendCode = chase.PendCode == "PC900";
            bool checkPendStatus = chase.PendStatusId == (int)DataContract.Enums.PendStatus.New
                                    || chase.PendStatusId == (int)DataContract.Enums.PendStatus.InProgress
                                    || chase.PendStatusId == (int)DataContract.Enums.PendStatus.Closed;

            if (checkChaseDataSet || checkChaseStatus || checkSelectedChase || (checkPendCode && checkPendStatus))
            {
                isDisabled = true;
            }

            return isDisabled;
        }
        /// <summary>
        /// Service is used to Copy Chart to Another Chase.
        /// </summary>
        /// <param name="sourceChaseId"></param>
        /// <param name="targetChaseId"></param>
        /// <param name="callerUserId"></param>
        public async Task<bool> CopyChartToAnotherChaseAsync(int sourceChaseId, List<int> targetChaseIds, int callerUserId)
        {
            var result = false;
            foreach (var targetChase in targetChaseIds)
            {

                result = await _chaseRepository.CopyChartToAnotherChaseAsync(sourceChaseId, targetChase, callerUserId);

            }

            return result;
        }

        /// <summary>
        /// Creates a chase
        /// </summary>
        /// <param name="chaseCreateRequest"></param>
        /// <param name="callerUserId"></param>
        /// <returns>The id of newly created chase and member-centric parent chaseId</returns>
        public async Task<ChaseCreateResponse> CreateChaseAsync(ChaseCreateRequest chaseCreateRequest, int callerUserId)
        {

            var response = await _chaseRepository
                                .CreateChaseAsync(chaseCreateRequest, callerUserId);

            return response;
        }

        /// <summary>
        /// Validate chase
        /// </summary>
        /// <param name="chaseCreateRequest"></param>
        /// <param name="callerUserId"></param>
        public async Task<string> ValidateNewChaseAsync(ChaseCreateRequest chaseCreateRequest, int callerUserId)
        {
            return await _chaseRepository.ValidateNewChaseAsync(chaseCreateRequest, callerUserId);

        }

        /// <summary>
        /// Checks to see if the chase key is unique for the given project
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="chaseKey"></param>
        /// <param name="callerUserId"></param>
        /// <returns>true if unique, false otherwise</returns>
        public async Task<bool> CheckIfChaseKeyIsUniqueAsync(int projectId, string chaseKey, int callerUserId)
        {
            bool isChaseKeyUnique = true;

            var result = await _chaseRepository
                                    .GetChaseDetailByChaseKeyAsync(projectId, chaseKey, callerUserId);

            if (result != null)
            {
                if (result
                        .ToList()
                        .Any(item => !string.IsNullOrEmpty(item.ChaseSourceAliasId) && item.ChaseSourceAliasId.Equals(chaseKey, StringComparison.OrdinalIgnoreCase)))
                {
                    isChaseKeyUnique = false;
                }
                else
                {
                    isChaseKeyUnique = true;
                }
            }

            return isChaseKeyUnique;
        }

        /// <summary>
        /// Gets list of attribute data related to a measure
        /// </summary>
        /// <param name="measureId"></param>
        /// <param name="callerUserId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<MeasureAttributeData>> GetAttributeDataByMeasureAsync(int measureId, int callerUserId)
        {
            IEnumerable<MeasureAttributeData> result = null;

            result = await _chaseRepository
                                .GetAttributeDataByMeasureAsync(measureId, callerUserId);

            return result;
        }

        /// <summary>
        /// Gets nlp processing results based on chase id.
        /// The data is based on results from catalytic
        /// </summary>
        /// <param name="chaseId"></param>
        /// <returns></returns>
        public async Task<CatalyticNLPOverreadDocumentSubmissionResponse> GetChaseNlpDataAsync(int chaseId)
        {
            CatalyticNLPOverreadDocumentSubmissionResponse response = null;
            var fileKey = string.Format(@"catalytic-nlp-processing/results/{0}/{0}.json", chaseId);

            try
            {
                // Get from S3 bucket
                string s3BucketName = Configuration
                           .GetSection("Keys:S3BucketForCatalyticNlpProcessingResults")
                           .GetSection("Value").Value;

                var s3Object = await _s3Service
                                    .GetObjectAsync(s3BucketName, fileKey);

                if (s3Object != null)
                {
                    response = JsonConvert
                                .DeserializeObject<CatalyticNLPOverreadDocumentSubmissionResponse>(s3Object);

                }
            }
            catch (Exception ex)
            {
                var parameters = new Dictionary<string, object>();
                parameters.Add("loginUserId", LoginUser.UserId.ToString());
                parameters.Add("chaseId", chaseId);
                parameters.Add("fileKey", fileKey);

                var message = JsonConvert.SerializeObject(new { Source = ex.Source, Parameters = parameters, Message = ex.Message }, Formatting.Indented, jsonSerializerSettings);

                // Log the exception as Warning
                await LogHelper
                        .LogAsWarningAsync(message)
                        .ConfigureAwait(false);
            }

            return response;
        }

        /// <summary>
        /// Gets nlp processing results based on chase id.
        /// The data is based on results from catalytic and user actions
        /// </summary>
        /// <param name="chaseId"></param>
        /// <returns></returns>
        public async Task<ChaseNlpData> GetChaseNlpSystemResultsAsync(int chaseId)
        {
            ChaseNlpData chaseNlpData = null;

            try
            {
                // Get from S3 bucket
                var result = await GetChaseNlpDataAsync(chaseId);

                if (result != null)
                {
                    if (result.Events != null && result.Events.Any())
                    {

                        chaseNlpData = new ChaseNlpData()
                        {
                            ChaseId = chaseId,
                            Numerators = new List<ChaseNlpNumerator>(),
                            ReviewedByUser = false
                        };

                        var numerators = new List<ChaseNlpNumerator>();

                        int totalMatch = 0;
                        int totalNoMatch = 0;
                        int totalPartialMatch = 0;

                        foreach (var item in result.Events)
                        {
                            int chaseNlpMatchResult = GetChaseNlpMatchResult(item.Matched);

                            if (chaseNlpMatchResult == (int)ChaseNlpMatchResult.Match)
                            {
                                totalMatch++;
                            }
                            else if (chaseNlpMatchResult == (int)ChaseNlpMatchResult.NoMatch)
                            {
                                totalNoMatch++;
                            }
                            else if (chaseNlpMatchResult == (int)ChaseNlpMatchResult.PartialMatch)
                            {
                                totalPartialMatch++;
                            }

                            var numerator = new ChaseNlpNumerator()
                            {
                                EntityTypeId            = item.EntityTypeId,
                                EntityTypeName          = item.EntityTypeName,
                                MatchResult             = chaseNlpMatchResult,
                                Result                  = string.Empty,
                                EventId                 = item.EventId,
                                EntityTypeDisplayOrder  = item.EntityTypeDisplayOrder,
                                BotPageNumber           = item.SuggestedPageNumber.ToString()
                            };

                            if (item.SupportingLocations != null && item.SupportingLocations.Any())
                            {
                                foreach (var supportingLocation in item.SupportingLocations)
                                {
                                    supportingLocation.Boxes = new DataContract.Catalytic.Box() { BoundingBox = supportingLocation.BoundingBox };
                                }

                                numerator.SupportingLocation = new DataContract.Catalytic.SupportingLocation() { Locations = item.SupportingLocations.ToList() };
                            }

                            foreach (var attribute in item.Attributes)
                            {
                                // set date of service
                                if (!string.IsNullOrEmpty(attribute.AttributeType)
                                     && attribute.AttributeType.Equals("date", StringComparison.OrdinalIgnoreCase))
                                {
                                    numerator.DateOfService = attribute.AttributeValue;
                                }

                                // set medical record page number
                                else if (!string.IsNullOrEmpty(attribute.AttributeType)
                                    && !string.IsNullOrEmpty(attribute.AttributeCode)
                                    && attribute.AttributeType.Equals("numeric", StringComparison.OrdinalIgnoreCase)
                                    && attribute.AttributeCode.ToUpper().Contains("CHARTPAGENUMBER"))
                                {
                                    numerator.MedicalRecordPageNumber = attribute.AttributeValue;
                                }
                                else
                                if (!string.IsNullOrEmpty(attribute.AttributeValue))
                                {
                                    numerator.Result += string.Format(@"{0}, ", attribute.AttributeValue);
                                }
                            }

                            // Add to the list of numerators
                            numerators.Add(numerator);
                        }

                        chaseNlpData.TotalMatch         = totalMatch;
                        chaseNlpData.TotalNoMatch       = totalNoMatch;
                        chaseNlpData.TotalPartialMatch  = totalPartialMatch;

                        // Set the numerators based on display order
                        chaseNlpData.Numerators = numerators
                                                    .OrderBy(item => item.EntityTypeDisplayOrder)
                                                    .ToList();
                    }
                    else
                    {
                        // Since no data entry was performed, return all avaiable events in our system as a Match

                        // Get events for a chase
                        var chaseNlpEventDataFromDb = await _chaseRepository
                                                                .GetChaseNlpEventDataAsync(chaseId);

                        if(chaseNlpEventDataFromDb != null && chaseNlpEventDataFromDb.Any())
                        {
                            int totalMatch          = chaseNlpEventDataFromDb.Count();
                            int totalNoMatch        = 0;
                            int totalPartialMatch   = 0;

                            chaseNlpData = new ChaseNlpData()
                            {
                                ChaseId             = chaseId,
                                Numerators          = new List<ChaseNlpNumerator>(),
                                ReviewedByUser      = false,
                                TotalMatch          = totalMatch,
                                TotalNoMatch        = totalNoMatch,
                                TotalPartialMatch   = totalPartialMatch
                            };

                            var numerators = new List<ChaseNlpNumerator>();

                            foreach (var item in chaseNlpEventDataFromDb)
                            {
                                numerators.Add(new ChaseNlpNumerator()
                                {
                                    EntityTypeId            = item.EntityTypeId,
                                    EntityTypeName          = item.EntityTypeName,
                                    EntityTypeDisplayOrder  = item.EntityTypeDisplayOrder,
                                    MatchResult             = (int)ChaseNlpMatchResult.Match
                                });
                            }

                            // Set the numerators based on display order
                            chaseNlpData.Numerators = numerators
                                                        .OrderBy(item => item.EntityTypeDisplayOrder)
                                                        .ToList();
                        }
                    }
                }

                if (chaseNlpData != null)
                {
                    // Get user actions from DB
                    var chaseNlpDataFromDb = await _chaseRepository
                                                        .GetChaseNlpDataAsync(chaseId);

                    if (chaseNlpDataFromDb != null)
                    {
                        var result2 = XmlHelper.GetFromXml<ChaseNlpData>(chaseNlpDataFromDb.NumeratorsAsXml);

                        // loop through chase nlp data and update 
                        // user actions based on database
                        foreach (var numerator in chaseNlpData.Numerators)
                        {
                            numerator.Accepted = result2
                                                .Numerators
                                                .Where(item => item.EntityTypeId == numerator.EntityTypeId)
                                                .Select(item => item.Accepted)
                                                .FirstOrDefault();
                        }

                        // Set the numerators based on display order
                        chaseNlpData.Numerators = chaseNlpData.Numerators
                                                    .OrderBy(item => item.EntityTypeDisplayOrder)
                                                    .ToList();

                        chaseNlpData.ReviewedByUser         = true;
                        chaseNlpData.SystemResultsReviewed  = chaseNlpDataFromDb.SystemResultsReviewed;
                    }

                    // Log the result
                    var chaseNlpDataAsString = JsonConvert.SerializeObject(chaseNlpData, Formatting.Indented, jsonSerializerSettings);

                    await LogHelper
                            .LogAsWarningAsync(chaseNlpDataAsString);
                }
            }
            catch (Exception ex)
            {
                var parameters = new Dictionary<string, object>();
                parameters.Add("loginUserId", LoginUser.UserId.ToString());
                parameters.Add("chaseId", chaseId);

                var message = JsonConvert.SerializeObject(new { Source = ex.Source, Parameters = parameters, Message = ex.Message }, Formatting.Indented, jsonSerializerSettings);

                // Log the exception as Warning
                await LogHelper
                        .LogAsWarningAsync(message)
                        .ConfigureAwait(false);
            }

            return chaseNlpData;
        }

        /// <summary>
        /// Returns the Nlp match enum based on the result
        /// </summary>
        /// <param name="matched"></param>
        /// <returns></returns>
        private int GetChaseNlpMatchResult(string matched)
        {
            int chaseNlpMatchResult = (int)ChaseNlpMatchResult.NoMatch;

            switch (matched.ToUpper())
            {
                case "YES":
                    chaseNlpMatchResult = (int)ChaseNlpMatchResult.Match;
                    break;

                case "NO":
                    chaseNlpMatchResult = (int)ChaseNlpMatchResult.NoMatch;
                    break;

                case "PARTIAL":
                    chaseNlpMatchResult = (int)ChaseNlpMatchResult.PartialMatch;
                    break;
                default:
                    break;
            }

            return chaseNlpMatchResult;
        }

        /// <summary>
        /// Saves the Nlp data from catalytic process
        /// </summary>
        /// <param name="chaseNlpData"></param>
        /// <returns></returns>
        public async Task SaveChaseNlpDataAsync(ChaseNlpData chaseNlpData)
        {
            await ValidateAsync(chaseNlpData);

            foreach (var item in chaseNlpData.Numerators)
            {
                item.Accepted = (item.Accepted == null) ? -1 : item.Accepted;
            }

            chaseNlpData.NumeratorsAsXml = XmlHelper.GetAsXml<ChaseNlpData>(chaseNlpData);

            if (!String.IsNullOrEmpty(chaseNlpData.NumeratorsAsXml)) {

                var newNumeratorXml = chaseNlpData.NumeratorsAsXml.Replace("<accepted>-1</accepted>", "<accepted>null</accepted>");
                chaseNlpData.NumeratorsAsXml = newNumeratorXml;

            }

            // Update OrganizationId
            chaseNlpData.OrganizationId = LoginUser.OrganizationId;
            chaseNlpData.CallerUserId   = LoginUser.UserId;

            await _chaseRepository
                    .SaveChaseNlpDataAsync(chaseNlpData, LoginUser.UserId);

            if (chaseNlpData.SystemResultsReviewed)
            {
                await ProcessSystemResultsAsync(chaseNlpData, LoginUser.UserId);

                // Log the final submission data
                var chaseNlpDataAsJson = JsonConvert.SerializeObject(chaseNlpData);

                await LogHelper
                        .LogAsWarningAsync(string.Format(@"Catalytic Nlp data was submitted for chaseId: {0} by userId: {1}. Data: {2}", chaseNlpData.ChaseId, LoginUser.UserId, chaseNlpDataAsJson));
            }
        }

        private async Task ValidateAsync(ChaseNlpData chaseNlpData)
        {
            string message = "Invalid data entry. Please make sure you have entered all values and try again.";

            if (chaseNlpData == null || chaseNlpData.Numerators == null || !chaseNlpData.Numerators.Any())
            {
                throw new BrokenRuleException(message);
            }
        }

        /// <summary>
        /// Gets list of pages for a given chase id
        /// </summary>
        /// <param name="chaseId"></param>
        /// <param name="documentTypeId"></param>
        /// <param name="begPage"></param>
        /// <param name="endPage"></param>
        /// <param name="callerUserId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<ChaseDocumentPage>> GetChaseDocumentPages(int chaseId, int documentTypeId, int begPage, int endPage, int callerUserId)
        {
            return await _chaseRepository
                            .GetChaseDocumentPages(chaseId, documentTypeId, begPage, endPage, callerUserId);

        }

        /// <summary>
        /// Determines if workflowStatusId is in range for Chart attachment
        /// </summary>
        /// <param name="workflowStatusId"></param>
        /// <returns></returns>
        public bool IsWorkflowStatusForChartAttach(int workflowStatusId)
        {
            return (workflowStatusId == (int)WorkflowStatus.ChartCollection
                || workflowStatusId == (int)WorkflowStatus.WaitingForChart
                || workflowStatusId == (int)WorkflowStatus.ChartQA
                || workflowStatusId == (int)WorkflowStatus.Abstraction);

        }

        /// <summary>
        /// Check if chases are assigned
        /// </summary>
        /// <param name="chaseIds"></param>
        /// <param name="callerUserId"></param>
        public async Task<bool> ValidateChasesForAssignmentAsync(IEnumerable<int> chaseIds, int callerUserId)
        {
            bool isChaseAssigned = false;

            ChaseQuerySearchCriteria model = new ChaseQuerySearchCriteria();
            model.ChaseIdsAsXml = GetChaseIdsXml(chaseIds);
            model.CallerUserId = callerUserId;

            var result = await _chaseRepository.GetChasesQueryListAsync(model);
            if (result != null)
            {
                isChaseAssigned = result.Any(x => !string.IsNullOrEmpty(x.AssignedTo) && !string.IsNullOrEmpty(x.AssignedTo.Trim()));
            }

            return isChaseAssigned;
        }

        private string GetChaseIdsXml(IEnumerable<int> chaseIds)
        {
            var chases = new ChaseItems(chaseIds);
            var chasesXml = XmlHelper.GetAsXml(chases);
            return chasesXml;
        }

        /// <summary>
        /// Sets bot page number based on date of service information
        /// </summary>
        /// <param name="chaseNlpData"></param>
        private void SetBotPageNumber(ChaseNlpData chaseNlpData)
        {
            if (chaseNlpData != null)
            {
                foreach (var numerator in chaseNlpData.Numerators)
                {
                    string botPageNumber = string.Empty;
                    if (numerator.SupportingLocation != null)
                    {
                        if (numerator.SupportingLocation.Locations != null && numerator.SupportingLocation.Locations.Any())
                        {
                            DateTime dateOfService;
                            botPageNumber = numerator
                                            .SupportingLocation
                                            .Locations
                                            .Where(item => (DateTime.TryParse(item.Text, out dateOfService) == true))
                                            .OrderBy(item => item.PageNumber)
                                            .Select(item => item.PageNumber.ToString())
                                            .FirstOrDefault();
                        }
                    }

                    if (!string.IsNullOrEmpty(botPageNumber))
                    {
                        numerator.BotPageNumber = botPageNumber;
                    }
                }
            }
        }

        /// <summary>
        /// Deletes an event based on chase id and event id
        /// </summary>
        /// <param name="chaseId"></param>
        /// <param name="entityId"></param>
        /// <param name="callerUserId"></param>
        /// <returns></returns>
        public async Task DeleteEventDataAsync(int chaseId, int entityId, int callerUserId)
        {
            await _chaseRepository
                        .DeleteEventDataAsync(chaseId, entityId, callerUserId);
        }

        /// <summary>
        /// Process events data based on nlp results
        /// </summary>
        /// <param name="chaseNlpData"></param>
        /// <param name="callerUserId"></param>
        /// <returns></returns>
        private async Task ProcessSystemResultsAsync(ChaseNlpData chaseNlpData, int callerUserId)
        {
            if (chaseNlpData != null)
            {
                var chaseId = chaseNlpData.ChaseId;

                // Delete events marked as NoMatch and Accepted
                if(chaseNlpData.Numerators != null && chaseNlpData.Numerators.Any())
                {
                    // Get list of all events where the result is a no match and user has accepted the result
                    var events = chaseNlpData
                                    .Numerators
                                    .Where(item => ((item.MatchResult == (int)ChaseNlpMatchResult.NoMatch)
                                                        && (item.Accepted == (int)ChaseNlpResultUserAction.Accepted)
                                                   //&& (item.EventDeleted == false)
                                                   ))
                                    .Select(item => item);

                    // Delete the data related to the event
                    foreach (var item in events)
                    {
                        if (!string.IsNullOrEmpty(item.EventId))
                        {
                            int eventId = Convert.ToInt32(item.EventId);

                            if (eventId > 0)
                            {
                                await DeleteEventDataAsync(chaseId, eventId, callerUserId);

                                await LogHelper
                                         .LogAsWarningAsync(string.Format(@"DeleteEventDataAsync() : ChaseId: {0}, EventId: {1}", chaseId, item.EventId));

                                // Mark the event as deleted
                                //item.EventDeleted = true;
                            }
                        }
                    }
                }

                // Add comments for submission of Hedis Nlp system results
                var commentText = "Bot System Results Submitted";
                await AddChaseCommentAsync(chaseId, commentText, callerUserId);
            }
        }


        /// <summary>
        /// Converts NLP SupportLocations data into Annotation markup
        /// </summary>
        /// <param name="chaseNlpAnnotationRequest"></param>
        /// <returns></returns>
        public async Task ConvertNLPSupportLocationsToAnnotationAsync(ChaseNlpAnnotationRequest chaseNlpAnnotationRequest)
        {
            await LogHelper.LogDebugAsync($"ConvertNLPSupportLocationsToAnnotationAsync. chaseNlpAnnotationRequest={JsonConvert.SerializeObject(chaseNlpAnnotationRequest)}");
            // Get current Annotations. NOTE: FE passes ChaseId in ChaseDocumentId argument for AnnotationService

            try
            {
                var currentAnnotation = GetCurrentAnnotation(chaseNlpAnnotationRequest.ChaseId);

                DeleteExistingNLPAnnotations(currentAnnotation);

                // Get all ChaseDocumentPages for this Chase
                var chaseDocumentPages = await this.GetChaseDocumentPages(chaseNlpAnnotationRequest.ChaseId, (int)DocumentType.MedicalRecord, 1, 99999, chaseNlpAnnotationRequest.CallerUserId);

                // Add new Annotations from SupporingtLocations
                foreach (var supportLocation in chaseNlpAnnotationRequest.SupportingLocations)
                {
                    await LogHelper.LogDebugAsync($"ConvertNLPSupportLocationsToAnnotationAsync. supportLocation={JsonConvert.SerializeObject(supportLocation)}");

                    // Only process for existing ChaseDocumentPage from MRCS.ChaseDocumentPage
                    var chaseDocumentPage = chaseDocumentPages.Where(r => r.PageNumber == supportLocation.PageNumber).FirstOrDefault();
                    if (chaseDocumentPage != null)
                    {

                        await LogHelper.LogDebugAsync($"ConvertNLPSupportLocationsToAnnotationAsync. Found chaseDocumentPage for supportLocation={JsonConvert.SerializeObject(supportLocation)}");

                        var chaseDocumentPageAnnotation = GetChaseDocumentPageAnnotation(currentAnnotation, chaseDocumentPage.DocumentPageID);

                        // Finally, add the NLP Annotation
                        chaseDocumentPageAnnotation.Annotations.Add(ConvertSupportLocationToAnnotation(supportLocation, chaseNlpAnnotationRequest.CallerUserId));
                    }
                    else
                    {
                        await LogHelper.LogDebugAsync($"ConvertNLPSupportLocationsToAnnotationAsync. Did not find chaseDocumentPage  for supportLocation={JsonConvert.SerializeObject(supportLocation)}");
                    }
                }

                await LogHelper.LogDebugAsync($"ConvertNLPSupportLocationsToAnnotationAsync. currentAnnotation={JsonConvert.SerializeObject(currentAnnotation)}");

                // Save currentAnnotation
                await _annotationService.UpdateChaseDocumentAnnotationAsync(currentAnnotation);
            }
            catch (Exception ex)
            {
                Dictionary<string, object> My_dict1 =
                       new Dictionary<string, object>();

                // Adding key/value pairs  
                // in the Dictionary 
                // Using Add() method 
                My_dict1.Add("Source", "ConvertNLPSupportLocationsToAnnotationAsync");
                My_dict1.Add("ChaseId", chaseNlpAnnotationRequest.ChaseId.ToString());
                My_dict1.Add("StackTrace", ex.StackTrace);

                await LogHelper.LogErrorAsync(ex, My_dict1);

            }
        }

        /// <summary>
        /// Get existing or create new chaseDocumentPageAnnotation for this chaseDocumentPage
        /// </summary>
        /// <param name="currentAnnotation"></param>
        /// <param name="documentPageID"></param>
        /// <returns></returns>
        private ChaseDocumentPageAnnotation GetChaseDocumentPageAnnotation(ChaseDocumentAnnotation currentAnnotation, int documentPageID)
        {
            var chaseDocumentPageAnnotation = currentAnnotation.chaseDocumentPages.Where(r => r.ChaseDocumentPageId == documentPageID).FirstOrDefault();
            if (chaseDocumentPageAnnotation == null)
            {
                chaseDocumentPageAnnotation = new ChaseDocumentPageAnnotation()
                {
                    ChaseDocumentPageId = documentPageID,
                    Annotations = new List<Annotation>()
                };
                currentAnnotation.chaseDocumentPages.Add(chaseDocumentPageAnnotation);
            }

            return chaseDocumentPageAnnotation;
        }

        /// <summary>
        /// Get ChaseDocumentAnnotation for a chaseDocumentId
        /// </summary>
        /// <param name="chaseDocumentId"></param>
        /// <returns></returns>
        private ChaseDocumentAnnotation GetCurrentAnnotation(int chaseDocumentId)
        {
            var currentAnnotation = _annotationService.GetChaseDocumentAnnotationAsync(chaseDocumentId).Result;
            if (currentAnnotation == null)
            {
                currentAnnotation = new ChaseDocumentAnnotation()
                {
                    ChaseDocumentId = chaseDocumentId,
                    chaseDocumentPages = new List<ChaseDocumentPageAnnotation>()
                };
            }

            return currentAnnotation;
        }

        /// <summary>
        ///  Delete existing NLP Annotations, NLP Annotations will be overwritten with incoming values
        /// </summary>
        /// <param name="currentAnnotation"></param>
        private void DeleteExistingNLPAnnotations(ChaseDocumentAnnotation currentAnnotation)
        {
            for (int i = currentAnnotation.chaseDocumentPages.Count - 1; i >= 0; i--)
            {
                var chaseDocumentAnnotation = currentAnnotation.chaseDocumentPages[i];

                // Remove all NLP Annotations
                chaseDocumentAnnotation.Annotations.RemoveAll(x => x.AnnotationSourceId == AnnotationSource.NLP);

                // If not Annotations left in chaseDocmentPage then remove Parent
                if (chaseDocumentAnnotation.Annotations.Count == 0)
                    currentAnnotation.chaseDocumentPages.RemoveAt(i);
            }
        }

        /// <summary>
        /// Convert from SupportLocation rectangle coordinates to proprietary Annotation coordinates
        /// </summary>
        /// <param name="supportLocation"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        private Annotation ConvertSupportLocationToAnnotation(DataContract.Catalytic.Location supportLocation, int userId)
        {
            Annotation annotation = new Annotation()
            {
                AnnotationSourceId = AnnotationSource.NLP,
                CreateDate = DateTime.Now,
                UserId = userId,
                AnnotationKey = !String.IsNullOrEmpty(supportLocation.EventId) ? supportLocation.EventId : (!String.IsNullOrEmpty(supportLocation.Measure) ? supportLocation.Measure : supportLocation.PageNumber.ToString())
            };

            // On FE Annotations coordinates are written based a fixed page Height. and pixles and widths
            int annotationPageHeight = 1558;

            // This is what Ben says will work:
            //startX:x0,
            //widthX: x1 - x0,
            //startY: y0 * 1560,
            //heightY: (y1 - y0) * 1560


            //decimal value wich is a multiplier percentage in FE
            annotation.StartX = supportLocation.BoundingBox[0].X;

            // decimal  value which is a multiplier percentage in FE
            annotation.WidthX = supportLocation.BoundingBox[1].X - supportLocation.BoundingBox[0].X;

            // pixel representing vertical starting pixel
            annotation.StartY = supportLocation.BoundingBox[0].Y * Convert.ToDecimal(annotationPageHeight);

            // Height in pixels
            annotation.HeightY = (supportLocation.BoundingBox[1].Y  - supportLocation.BoundingBox[0].Y) * Convert.ToDecimal(annotationPageHeight);

            return annotation;
        }

        /// <summary>
        /// Perform additional tasks needed not done in the DB after chase moved back
        /// </summary>
        /// <param name="chaseId"></param>
        /// <param name="callerUserId"></param>
        /// <returns></returns>
        public async Task<bool> OnChaseMoveBackAsync(int chaseId, int callerUserId) {
            var deletedChaseAnnotations = false;

            var chaseDetail = await this.GetChaseDetailAsync(chaseId, callerUserId);

            if (chaseDetail!=null)
            {

                var workflowStatus = chaseDetail.WorkflowStatusId;

                // Check for Delete NLP Annotations
                if ((int)workflowStatus <= (int)WorkflowStatus.Abstraction) {

                    await _annotationService
                            .DeleteChaseAnnotationsAsync(chaseId, (int)AnnotationSource.NLP);

                    deletedChaseAnnotations = true;
                }
            }

            return deletedChaseAnnotations;
        }

        /// <summary>
        /// Perform additional tasks needed not done in the DB after Bulk Move Back
        /// </summary>
        /// <param name="chaseIds">List of ChaseId's</param>
        /// <param name="callerUserId"></param>
        /// <returns></returns>
        public async Task OnBulkChaseMoveBackAsync(IEnumerable<ValidateBulkChasesResponse> bulkChasesResponses, int callerUserId) {

            // Get DB WorkflowItems  for Status comparison from DB because ValidateBulkChasesResponse only has string values
            var workflowStatusItems = await _chaseRepository.GetWorkflowStatusesAsync();
            await DeleteNLPAnnotations(bulkChasesResponses, workflowStatusItems);


        }

        private async Task DeleteNLPAnnotations(IEnumerable<ValidateBulkChasesResponse> bulkChasesResponses, IEnumerable<WorkflowStatusItem> workflowStatusItems) {

            List<int> listDeleteChaseNlpAnnotations = bulkChasesResponses.Where(chase => ConvertToWorkflowStatusId(workflowStatusItems,chase.NewWorkflowStatusName) <= (int)WorkflowStatus.Abstraction).Select(s => s.ChaseId).ToList();
            await _annotationService.BulkDeleteChaseAnnotations(listDeleteChaseNlpAnnotations, (int)AnnotationSource.NLP);

        }

        /// <summary>
        /// Returns WorkflowStatusId for matching workflowStatusName
        /// </summary>
        /// <param name="workflowStatusName"></param>
        /// <returns></returns>
        public static  int ConvertToWorkflowStatusId(IEnumerable<WorkflowStatusItem> workflowStatusItems,string workflowStatusName)
        {
            return workflowStatusItems.Where(w => w.WorkflowStatusName == workflowStatusName).Select(s => s.WorkflowStatusId).FirstOrDefault();

        }

        /// <summary>
        /// Gets list of numerator related to a measure
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="measureId"></param>
        /// <param name="callerUserId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<NumeratorData>> GetNumeratorListByMeasureAsync(int projectId, int measureId, int callerUserId)
        {
            IEnumerable<NumeratorData> result = null;

            result = await _chaseRepository
                                .GetNumeratorListByMeasureAsync(projectId, measureId);

            return result;
        }

        public async Task UpdateChaseNLPRequestResponseDataAsync(NlpRequestResponseDataLog nlpRequestResponseDataLog)
        {
            await _chaseRepository.UpdateChaseNLPRequestResponseDataAsync(nlpRequestResponseDataLog);

        }

        /// <summary>
        /// Gets record from NlpRequest table based on chase id
        /// </summary>
        /// <param name="chaseId"></param>
        /// <param name="requestStatusId"></param>
        /// <returns></returns>
        public async Task<NlpRequest> GetNlpRequestDataAsync(int chaseId, int requestStatusId)
        {
            var result = await _chaseRepository
                                    .GetNlpRequestDataAsync(chaseId, requestStatusId);

            return result;
        }

        /// <summary>
        /// Get risk nlp data including system reults
        /// </summary>
        /// <param name="chaseId"></param>
        /// <param name="callerUserId"></param>
        /// <returns></returns>
        public async Task<RiskNlpData> GetRiskNlpSystemResultsAsync(int chaseId, int callerUserId)
        {
            RiskNlpData riskNlpData = null;

            var chaseResult = await _chaseRepository
                                        .GetChaseDetailAsync(chaseId, callerUserId);

            if(chaseResult != null)
            {
                var projectId = chaseResult.ProjectId;

                var serviceTypeRequired = await IsServiceTypeRequiredAsync(projectId);
                var vrcRequired         = await IsVrcRequiredAsync(projectId);

                riskNlpData = new RiskNlpData()
                {
                    ChaseId                 = chaseId,
                    ServiceTypeRequired     = serviceTypeRequired,
                    VrcRequired             = vrcRequired,
                    VerificationReasonCodes = new List<VerificationReasonCode>(),
                    ServiceTypes            = new List<ServiceType>(),
                    Diagnoses               = await GetDiagnosisDataAsync(chaseId, callerUserId)
                };

                // set list of vrc's only if it is set to be required at the the project level
                if (vrcRequired)
                {
                    riskNlpData.VerificationReasonCodes = await GetVerificationReasonCodesAsync();
                }

                // set list of servicetype's only if it is set to be required at the the project level
                if (serviceTypeRequired)
                {
                    riskNlpData.ServiceTypes = await GetServiceTypesAsync();
                }
            }

            return riskNlpData;
        }

        /// <summary>
        /// Is Vrc required for a given project
        /// </summary>
        /// <param name="projectId"></param>
        /// <returns></returns>
        private async Task<bool> IsVrcRequiredAsync(int projectId)
        {
            var IsVrcRequired = false;

            _projectConfigurationService.LoginUser = LoginUser;

            var attributes = await _projectConfigurationService
                                        .GetProjectConfigurationAttributeAsync(projectId, (int)MrcsAttribute.ValidationReasonCodeRequired);

            if (attributes != null && attributes.Any())
            {
                IsVrcRequired = attributes
                                    .First()
                                    .AttributeValue == "1" ? true : false;
            }

            return IsVrcRequired;
        }

        /// <summary>
        /// Is service type required for a given project
        /// </summary>
        /// <param name="projectId"></param>
        /// <returns></returns>
        private async Task<bool> IsServiceTypeRequiredAsync(int projectId)
        {
            var IsServiceTypeRequired = false;

            _projectConfigurationService.LoginUser = LoginUser;

            var attributes = await _projectConfigurationService
                                        .GetProjectConfigurationAttributeAsync(projectId, (int)MrcsAttribute.EncounterTypeRequired);

            if (attributes != null && attributes.Any())
            {
                IsServiceTypeRequired = attributes
                                            .First()
                                            .AttributeValue == "1" ? true : false;
            }

            return IsServiceTypeRequired;
        }

        /// <summary>
        /// Get list of service types
        /// </summary>
        /// <returns></returns>
        private async Task<IEnumerable<ServiceType>> GetServiceTypesAsync()
        {
            var serviceTypes = new List<ServiceType>();

            var attributeOptions = await _entityService
                                            .GetAttributeOptions("EncounterType");

            if(attributeOptions != null && attributeOptions.Any())
            {
                foreach (var item in attributeOptions)
                {
                    serviceTypes.Add(new ServiceType() { ItemId = item.Value, ItemText = item.Text });
                }
            }

            return serviceTypes;
        }

        /// <summary>
        /// Get list of Verification Reason Codes
        /// </summary>
        /// <returns></returns>
        private async Task<IEnumerable<VerificationReasonCode>> GetVerificationReasonCodesAsync()
        {
            var verificationReasonCodes = new List<VerificationReasonCode>();

            var attributeOptions = await _entityService
                                            .GetAttributeOptions("ValidationReasonCode");

            if (attributeOptions != null && attributeOptions.Any())
            {
                foreach (var item in attributeOptions)
                {
                    verificationReasonCodes.Add(new VerificationReasonCode() { Id = item.Value,  Code = item.Text });
                }
            }

            return verificationReasonCodes;
        }

        /// <summary>
        /// Gets list of diagnosis data including user actions base don system results
        /// </summary>
        /// <param name="chaseId"></param>
        /// <param name="callerUserId"></param>
        /// <returns></returns>
        private async Task<List<RiskNlpDiagnosisData>> GetDiagnosisDataAsync(int chaseId, int callerUserId)
        {
            //TODO: Get data from NlpEntity and NlpEntityAttributeTable

            var diagnoses = new List<RiskNlpDiagnosisData>()
                                {
                                    new RiskNlpDiagnosisData()
                                    {
                                        EncounterId             = 1297,
                                        ClaimId                 = "C00001",
                                        BotPageNumber           = "0",
                                        EncouterType            = "Claim DOS",
                                        DateOfServiceFrom       = "07/01/2020",
                                        DateOfServiceThrough    = "07/01/2020",
                                        DiagnosisId             = 242342,
                                        DiagnosisCode           = "E1151",
                                        DiagnosisDescription    = "Type 2 diabetes mellitus with diabetic peripheral angiopathy without gangrene",
                                        Hcc                     = "18,108",
                                        ProviderName            = "Bernardo Boyd",
                                        SystemResult            = "No Match",
                                        Encounter               = "Y"
                                    },
                                    new RiskNlpDiagnosisData()
                                    {
                                        EncounterId             = 1297,
                                        ClaimId                 = "C00001",
                                        BotPageNumber           = "6",
                                        EncouterType            = "Claim DOS",
                                        DateOfServiceFrom       = "07/01/2020",
                                        DateOfServiceThrough    = "07/01/2020",
                                        DiagnosisId             = 1234,
                                        DiagnosisCode           = "E66",
                                        DiagnosisDescription    = "OVERWEIGHT AND OBESITY",
                                        Hcc                     = string.Empty,
                                        ProviderName            = "Bernardo Boyd",
                                        SystemResult            = "Match",
                                        Encounter               = "Y",
                                        Accepted                = 0,
                                        ServiceType             = "10",
                                        VerificationReasonCodes = "21,31",
                                        SupportingLocation      = new DataContract.EzDI.SupportingLocation()
                                                                    {
                                                                        Locations = new List<DataContract.EzDI.Location>()
                                                                        {
                                                                            new DataContract.EzDI.Location()
                                                                            {
                                                                                EventId         = "12345",
                                                                                DocumentPageId  = 23456,
                                                                                PageNumber      = 6,
                                                                                Text            = "blood pressure",
                                                                                BoundingBox     = new List<DataContract.EzDI.BoundingBox>()
                                                                                {
                                                                                    new DataContract.EzDI.BoundingBox()
                                                                                    {
                                                                                        X = 0.45775536M,
                                                                                        Y = 0.8345324M
                                                                                    },
                                                                                    new DataContract.EzDI.BoundingBox()
                                                                                    {
                                                                                        X = 0.48675916M,
                                                                                        Y = 0.85071945M
                                                                                    }
                                                                                }
                                                                            }
                                                                        }
                                                                    }
                                    },
                                    new RiskNlpDiagnosisData()
                                    {
                                        EncounterId             = 1297,
                                        ClaimId                 = "C00001",
                                        BotPageNumber           = "6",
                                        EncouterType            = "Claim Net New",
                                        DateOfServiceFrom       = "07/01/2020",
                                        DateOfServiceThrough    = "07/01/2020",
                                        DiagnosisId             = null,
                                        DiagnosisCode           = "M05",
                                        DiagnosisDescription    = "RA WITH RHEUMATOID FACTOR",
                                        Hcc                     = string.Empty,
                                        ProviderName            = "Bernardo Boyd",
                                        SystemResult            = "Add",
                                        Encounter               = "Y"
                                    },
                                    new RiskNlpDiagnosisData()
                                    {
                                        EncounterId             = 1297,
                                        ClaimId                 = "C00001",
                                        BotPageNumber           = "10",
                                        EncouterType            = "Claim Net New",
                                        DateOfServiceFrom       = "08/01/2020",
                                        DateOfServiceThrough    = "08/01/2020",
                                        DiagnosisId             = null,
                                        DiagnosisCode           = "B15",
                                        DiagnosisDescription    = "ACUTE HEPATITIS A",
                                        Hcc                     = string.Empty,
                                        ProviderName            = "N/A",
                                        SystemResult            = "Add",
                                        Encounter               = "Y"
                                    },
                                    new RiskNlpDiagnosisData()
                                    {
                                        EncounterId             = 1297,
                                        ClaimId                 = "C00001",
                                        BotPageNumber           = "14",
                                        EncouterType            = "Claim Net New",
                                        DateOfServiceFrom       = "08/01/2020",
                                        DateOfServiceThrough    = "08/01/2020",
                                        DiagnosisId             = null,
                                        DiagnosisCode           = "F14.1",
                                        DiagnosisDescription    = "N/A",
                                        Hcc                     = string.Empty,
                                        ProviderName            = "N/A",
                                        SystemResult            = "Add",
                                        Encounter               = "Y"
                                    }
                                };

            return diagnoses;
        }

        /// <summary>
        ///  This API will return list of Chases.
        /// </summary>
        /// <param name="chaseSearchCriteria"></param>
        public async Task<IEnumerable<ChaseSearchResult>> ChaseTagSearchAsync(ChaseSearchCriteria chaseSearchCriteria)
        {
            var chaseSearchResult = await _chaseRepository.ChaseTagSearchAsync(chaseSearchCriteria);
            return chaseSearchResult;
        }

        /// <summary>
        /// Returns risk chase data based on chase id.
        /// </summary>
        /// <param name="chaseId"></param>
        /// <param name="callerUserId"></param>
        /// <param name="workflowStatus"></param>
        public async Task<ChaseDataSet> GetRiskChaseDataAsync(int chaseId, int callerUserId, string workflowStatus = null)
        {
            var result = await _chaseRepository
                                    .GetRiskChaseDetailAsync(chaseId, callerUserId, workflowStatus);

            var getChaseData = SortChaseData(result);

            return getChaseData;
        }

        /// <summary>
        /// sort chase data
        /// </summary>
        /// <param name="data"></param>
        public ChaseDataSet SortChaseData(RiskChaseData data)
        {
            int encounter = Convert.ToInt32(EntityType.Encounter);
            int fromDate = Convert.ToInt32(MrcsAttribute.StartDate);
            List<RiskEntity> sortData = new List<RiskEntity>();
            List<RiskEntity> unSortData = new List<RiskEntity>();
            foreach (var chaseData in data.ChaseData)
            {
                if (chaseData.Attributes.Any(c => c.AttributeId == fromDate) && chaseData.EntityTypeId == encounter)
                {
                    if (chaseData.Diagnoses != null && chaseData.Diagnoses.Any(c => c.Attributes != null) && chaseData.Diagnoses.Any(c => c.Attributes.Any(d => d.AttributeId == fromDate)))
                    {
                        chaseData.Diagnoses = chaseData
                        .Diagnoses
                        .Where(e => e.Attributes != null && e.Attributes.Any())
                        .OrderByDescending(c => Convert.ToDateTime(c.Attributes.Find(d => d.AttributeId == fromDate).Value))
                        .ToList();
                    }

                    sortData.Add(chaseData);
                }
                else
                {
                    unSortData.Add(chaseData);
                }
            }
            if (sortData.Count() > 0)
            {
               sortData = sortData.OrderBy(c => Convert.ToDateTime(c.Attributes.Find(c => c.AttributeId == fromDate).Value)).ToList();
            }
            unSortData.ForEach(c => sortData.Add(c));

            return new ChaseDataSet
            {
                ChaseId = data.ChaseId,
                ChaseSourceAliasId = data.ChaseSourceAliasId,
                ProjectId = data.ProjectId,
                ProjectName = data.ProjectName,
                ProjectTypeId = data.ProjectTypeId,
                ProjectTypeName = data.ProjectTypeName,
                MeasureYear = data.MeasureYear,
                MeasureCode = data.MeasureCode,
                WorkflowStatusId = data.WorkflowStatusId,
                WorkflowStatusName = data.WorkflowStatusName,
                Providers = data.Providers.ToList(),
                Data = sortData,
                DisplayNlpResults = data.DisplayNlpResults,
                OcrDataAvailable = data.OcrDataAvailable
            };
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
        public async Task<DocumentPageNlpMatches> GetNlpHighlightsByDiagnosisAsync(int chaseId, int? encounterId, int? diagnosisId, string diagnosisCode, string dosFrom, string dosThrough)
        {
            DocumentPageNlpMatches documentPageNlpMatches = null;
            List<NlpHighlight> nlpHighlights = new List<NlpHighlight>();

            // Get the Response JSON from NlpRequest table based on ChaseID
            var nlpRequest = await GetNlpRequestDataAsync(chaseId, (int)NlpRequestStatus.Completed);

            if (nlpRequest != null && !string.IsNullOrEmpty(nlpRequest.ResponseDetail))
            {
                var ezDIResponse = JsonConvert.DeserializeObject<RiskNlpSubmissionResponse>(nlpRequest.ResponseDetail);

                if (ezDIResponse != null && ezDIResponse.Encounters != null)
                {
                    IEnumerable<RiskNlpResponseEncounter> encounters = null;

                    // First, try to get list of encounters based on encounterid
                    if (encounterId != null && encounterId.HasValue)
                    {
                        encounters = ezDIResponse
                                            .Encounters
                                            .Where(item => item.EncounterId == encounterId);
                    }

                    // No encounters based on encounterid, get the list of encounters based on date of service from and through
                    if (!(encounters != null && encounters.Any()))
                    {
                        encounters = ezDIResponse
                                            .Encounters
                                            .Where(item => (item.DOSFrom == dosFrom && item.DOSThrough == dosThrough));
                    }

                    foreach (var encounter in encounters)
                    {
                        var diagnoses = encounter                                          
                                            .Diagnosis
                                            .Where(item => item.Code.Equals(diagnosisCode, StringComparison.OrdinalIgnoreCase));

                        if (diagnoses != null && diagnoses.Any())
                        {
                            foreach (var item in diagnoses)
                            {
                                int documentPageId  = item.DocumentPageId;
                                string diagnosisDOS = item.DOSFrom;
                                int? dOSPageNumber  = item.DOSPageNumber;

                                if (item != null && item.Evidences != null && item.Evidences.Any())
                                {
                                    var evidences = item
                                                    .Evidences
                                                    .Where(evidence => evidence.Status != null && evidence.Status.Equals("POSITIVE", StringComparison.OrdinalIgnoreCase));

                                    if (evidences != null && evidences.Any())
                                    {
                                        foreach (var evidence in evidences.OrderBy(item => item.PageNo))
                                        {
                                            List<OCRBoundingBox> ocrBoundingBoxes = null;

                                            if (evidence.BoundingBox != null && evidence.BoundingBox.Any())
                                            {
                                                ocrBoundingBoxes = new List<OCRBoundingBox>();
                                                OCRBoundingBox topLeftBoundgingBox = null;
                                                OCRBoundingBox bottomRightBoundgingBox = null;

                                                // Get first cordinate from first element
                                                var firstBoundingBox = evidence
                                                                            .BoundingBox
                                                                            .FirstOrDefault();

                                                if (firstBoundingBox != null)
                                                {
                                                    var boundingBox = firstBoundingBox.FirstOrDefault();

                                                    if (boundingBox != null)
                                                    {
                                                        topLeftBoundgingBox = new OCRBoundingBox() { x = boundingBox.X, y = boundingBox.Y };
                                                        ocrBoundingBoxes.Add(topLeftBoundgingBox);
                                                    }
                                                }

                                                // Get last cordinate from last element
                                                var lastBoundingBox = evidence
                                                                        .BoundingBox
                                                                        .LastOrDefault();

                                                if (lastBoundingBox != null)
                                                {
                                                    var boundingBox = lastBoundingBox.LastOrDefault();

                                                    if (boundingBox != null)
                                                    {
                                                        bottomRightBoundgingBox = new OCRBoundingBox() { x = boundingBox.X, y = boundingBox.Y };
                                                        ocrBoundingBoxes.Add(bottomRightBoundgingBox);
                                                    }
                                                }
                                            }

                                            var nlpHighlight = new NlpHighlight()
                                            {
                                                PageNumber      = evidence.PageNo,
                                                Text            = evidence.Text,
                                                Status          = evidence.Status,
                                                DocumentPageId  = documentPageId,
                                                DiagnosisDOS    = diagnosisDOS,
                                                DOSPageNumber   = dOSPageNumber,
                                                BoundingBoxes   = ocrBoundingBoxes
                                            };

                                            nlpHighlights.Add(nlpHighlight);
                                        }
                                    }
                                }

                                // Exclude provider details from evidence for now
                                //// Add Provider details as evidences
                                //if (item != null && item.ProviderDetails != null && item.ProviderDetails.Any())
                                //{
                                //    foreach (var providerDetail in item.ProviderDetails)
                                //    {
                                //        List<OCRBoundingBox> ocrBoundingBoxes = null;

                                //        if (providerDetail.BoundingBox != null && providerDetail.BoundingBox.Any())
                                //        {
                                //            ocrBoundingBoxes = new List<OCRBoundingBox>();

                                //            OCRBoundingBox topLeftBoundgingBox = null;
                                //            OCRBoundingBox bottomRightBoundgingBox = null;

                                //            // Get first cordinate from first element
                                //            var firstBoundingBox = providerDetail
                                //                                        .BoundingBox
                                //                                        .FirstOrDefault();

                                //            if (firstBoundingBox != null)
                                //            {
                                //                var boundingBox = firstBoundingBox.FirstOrDefault();

                                //                if (boundingBox != null)
                                //                {
                                //                    topLeftBoundgingBox = new OCRBoundingBox() { x = boundingBox.X, y = boundingBox.Y };
                                //                    ocrBoundingBoxes.Add(topLeftBoundgingBox);
                                //                }
                                //            }

                                //            // Get last cordinate from last element
                                //            var lastBoundingBox = providerDetail
                                //                                    .BoundingBox
                                //                                    .LastOrDefault();

                                //            if (lastBoundingBox != null)
                                //            {
                                //                var boundingBox = lastBoundingBox.LastOrDefault();

                                //                if (boundingBox != null)
                                //                {
                                //                    bottomRightBoundgingBox = new OCRBoundingBox() { x = boundingBox.X, y = boundingBox.Y };
                                //                    ocrBoundingBoxes.Add(bottomRightBoundgingBox);
                                //                }
                                //            }
                                //        }

                                //        var nlpHighlight = new NlpHighlight()
                                //        {
                                //            PageNumber          = providerDetail.PageNo,
                                //            Text                = providerDetail.Text,
                                //            Status              = "POSITIVE",
                                //            DocumentPageId      = documentPageId,
                                //            BoundingBoxes       = ocrBoundingBoxes,
                                //            IsProviderEvidence  = true
                                //        };

                                //        nlpHighlights.Add(nlpHighlight);
                                //    }
                                //}
                            }
                        }

                    }
                }

                documentPageNlpMatches = new DocumentPageNlpMatches()
                {
                    NlpMatches = nlpHighlights
                };
            }

            return documentPageNlpMatches;
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
        public async Task<DocumentPageNlpMatches> GetNlpDosHighlightsByDiagnosisAsync(int chaseId, int? encounterId, int? diagnosisId, string diagnosisCode, string dosFrom, string dosThrough)
        {
            DocumentPageNlpMatches documentPageNlpMatches = null;
            List<NlpHighlight> nlpHighlights = new List<NlpHighlight>();

            // Get the Response JSON from NlpRequest table based on ChaseID
            var nlpRequest = await GetNlpRequestDataAsync(chaseId, (int)NlpRequestStatus.Completed);

            if (nlpRequest != null && !string.IsNullOrEmpty(nlpRequest.ResponseDetail))
            {
                var ezDIResponse = JsonConvert.DeserializeObject<RiskNlpSubmissionResponse>(nlpRequest.ResponseDetail);

                if (ezDIResponse != null && ezDIResponse.Encounters != null)
                {
                    IEnumerable<RiskNlpResponseEncounter> encounters = null;

                    // First, try to get list of encounters based on encounterid
                    if (encounterId != null && encounterId.HasValue)
                    {
                        encounters = ezDIResponse
                                            .Encounters
                                            .Where(item => item.EncounterId == encounterId);
                    }

                    // No encounters based on encounterid, get the list of encounters based on date of service from and through
                    if (!(encounters != null && encounters.Any()))
                    {
                        encounters = ezDIResponse
                                            .Encounters
                                            .Where(item => (item.DOSFrom == dosFrom && item.DOSThrough == dosThrough));
                    }

                    foreach (var encounter in encounters)
                    {
                        var diagnoses = encounter
                                            .Diagnosis
                                            .Where(item => item.Code.Equals(diagnosisCode, StringComparison.OrdinalIgnoreCase));

                        if (diagnoses != null && diagnoses.Any())
                        {
                            foreach (var item in diagnoses)
                            {
                                int documentPageId  = item.DocumentPageId;
                                string diagnosisDOS = item.DOSFrom;
                                int? dOSPageNumber  = item.DOSPageNumber;

                                if (item != null && item.DOSEvidences != null && item.DOSEvidences.Any())
                                {
                                    var dosEvidences = item
                                                    .DOSEvidences
                                                    .Where(evidence => evidence.PageNo == dOSPageNumber);

                                    if (dosEvidences != null && dosEvidences.Any())
                                    {
                                        List<OCRBoundingBox> ocrBoundingBoxes = null;
                                        var dosEvidence = dosEvidences.FirstOrDefault();

                                        if (dosEvidence != null && dosEvidence.BoundingBox != null && dosEvidence.BoundingBox.Any())
                                        {
                                            ocrBoundingBoxes = new List<OCRBoundingBox>();
                                            OCRBoundingBox topLeftBoundgingBox = null;
                                            OCRBoundingBox bottomRightBoundgingBox = null;

                                            // Get first cordinate from first element
                                            var firstBoundingBox = dosEvidence
                                                                        .BoundingBox
                                                                        .FirstOrDefault();

                                            if (firstBoundingBox != null)
                                            {
                                                var boundingBox = firstBoundingBox.FirstOrDefault();

                                                if (boundingBox != null)
                                                {
                                                    topLeftBoundgingBox = new OCRBoundingBox() { x = boundingBox.X, y = boundingBox.Y };
                                                    ocrBoundingBoxes.Add(topLeftBoundgingBox);
                                                }
                                            }

                                            // Get last cordinate from last element
                                            var lastBoundingBox = dosEvidence
                                                                    .BoundingBox
                                                                    .LastOrDefault();

                                            if (lastBoundingBox != null)
                                            {
                                                var boundingBox = lastBoundingBox.LastOrDefault();

                                                if (boundingBox != null)
                                                {
                                                    bottomRightBoundgingBox = new OCRBoundingBox() { x = boundingBox.X, y = boundingBox.Y };
                                                    ocrBoundingBoxes.Add(bottomRightBoundgingBox);
                                                }
                                            }
                                        }

                                        var nlpHighlight = new NlpHighlight()
                                        {
                                            PageNumber      = dosEvidence.PageNo,
                                            Text            = dosEvidence.Text,
                                            DocumentPageId  = documentPageId,
                                            DiagnosisDOS    = diagnosisDOS,
                                            DOSPageNumber   = dOSPageNumber,
                                            BoundingBoxes   = ocrBoundingBoxes
                                        };

                                        nlpHighlights.Add(nlpHighlight);
                                    }
                                }
                            }
                        }

                    }
                }

                documentPageNlpMatches = new DocumentPageNlpMatches()
                {
                    NlpMatches = nlpHighlights
                };
            }

            return documentPageNlpMatches;
        }

        /// <summary>
        /// Returns list of NlpHighlights identified as exclusions based on chase id
        /// </summary>
        /// <param name="chaseId"></param>
        /// <returns></returns>
        public async Task<DocumentPageNlpMatches> GetNegativeExclusionHighlightsAsync(int chaseId)
        {
            DocumentPageNlpMatches documentPageNlpMatches = null;
            List<NlpHighlight> nlpHighlights = new List<NlpHighlight>();

            // Get the Response JSON from NlpRequest table based on ChaseID
            var nlpRequest = await GetNlpRequestDataAsync(chaseId, (int)NlpRequestStatus.Completed);

            if (nlpRequest != null && !string.IsNullOrEmpty(nlpRequest.ResponseDetail))
            {
                var ezDIResponse = JsonConvert.DeserializeObject<RiskNlpSubmissionResponse>(nlpRequest.ResponseDetail);

                if (ezDIResponse != null)
                {
                    foreach (var encounter in ezDIResponse.Encounters)
                    {
                        var diagnoses = encounter
                                            .Diagnosis;

                        if (diagnoses != null && diagnoses.Any())
                        {
                            foreach (var item in diagnoses)
                            {
                                int documentPageId  = item.DocumentPageId;
                                string diagnosisDOS = item.DOSFrom;
                                int? dOSPageNumber  = item.DOSPageNumber;

                                if (item != null && item.Evidences.Any())
                                {
                                    var evidences = item
                                                    .Evidences
                                                    .Where(evidence => evidence.Status != null && evidence.Status.Equals("NEGATIVE", StringComparison.OrdinalIgnoreCase));

                                    if (evidences != null && evidences.Any())
                                    {
                                        foreach (var evidence in evidences)
                                        {
                                            List<OCRBoundingBox> ocrBoundingBoxes = null;

                                            if (evidence.BoundingBox != null && evidence.BoundingBox.Any())
                                            {
                                                ocrBoundingBoxes = new List<OCRBoundingBox>();
                                                OCRBoundingBox topLeftBoundgingBox = null;
                                                OCRBoundingBox bottomRightBoundgingBox = null;

                                                // Get first cordinates from first element
                                                var firstBoundingBox = evidence
                                                                            .BoundingBox
                                                                            .FirstOrDefault();

                                                if (firstBoundingBox != null)
                                                {
                                                    var boundingBox = firstBoundingBox.FirstOrDefault();

                                                    if (boundingBox != null)
                                                    {
                                                        topLeftBoundgingBox = new OCRBoundingBox() { x = boundingBox.X, y = boundingBox.Y };
                                                        ocrBoundingBoxes.Add(topLeftBoundgingBox);
                                                    }
                                                }

                                                // Get last cordinates from last element
                                                var lastBoundingBox = evidence
                                                                        .BoundingBox
                                                                        .LastOrDefault();

                                                if (lastBoundingBox != null)
                                                {
                                                    var boundingBox = lastBoundingBox.LastOrDefault();

                                                    if (boundingBox != null)
                                                    {
                                                        bottomRightBoundgingBox = new OCRBoundingBox() { x = boundingBox.X, y = boundingBox.Y };
                                                        ocrBoundingBoxes.Add(bottomRightBoundgingBox);
                                                    }
                                                }
                                            }

                                            var nlpHighlight = new NlpHighlight()
                                            {
                                                PageNumber      = evidence.PageNo,
                                                Text            = evidence.Text,
                                                Status          = evidence.Status,
                                                DocumentPageId  = documentPageId,
                                                DiagnosisDOS    = diagnosisDOS,
                                                DOSPageNumber   = dOSPageNumber,
                                                BoundingBoxes   = ocrBoundingBoxes
                                            };

                                            nlpHighlights.Add(nlpHighlight);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                documentPageNlpMatches = new DocumentPageNlpMatches()
                {
                    NlpMatches = nlpHighlights
                };
            }

            return documentPageNlpMatches;
        }

        /// <summary>
        /// Returns list of NlpHighlights identified as templates based on chase id
        /// </summary>
        /// <param name="chaseId"></param>
        /// <returns></returns>
        public async Task<DocumentPageNlpMatches> GetTemplateHighlightsAsync(int chaseId)
        {
            DocumentPageNlpMatches documentPageNlpMatches = null;
            List<NlpHighlight> nlpHighlights = new List<NlpHighlight>();

            // Get the Response JSON from NlpRequest table based on ChaseID
            var nlpRequest = await GetNlpRequestDataAsync(chaseId, (int)NlpRequestStatus.Completed);

            if (nlpRequest != null && !string.IsNullOrEmpty(nlpRequest.ResponseDetail))
            {
                var ezDIResponse = JsonConvert.DeserializeObject<RiskNlpSubmissionResponse>(nlpRequest.ResponseDetail);

                if (ezDIResponse != null && ezDIResponse.NLPSections != null && ezDIResponse.NLPSections.Any())
                {
                    foreach (var nlpSection in ezDIResponse.NLPSections)
                    {
                        int documentPageId  = nlpSection.DocumentPageId;
                        int pageNumber      = nlpSection.PageNumber;


                        if (nlpSection != null && nlpSection.Sections != null && nlpSection.Sections.Any())
                        {
                            foreach (var section in nlpSection.Sections)
                            {
                                List<OCRBoundingBox> ocrBoundingBoxes = null;

                                if (section.BoundingBox != null && section.BoundingBox.Any())
                                {
                                    ocrBoundingBoxes = new List<OCRBoundingBox>();
                                    OCRBoundingBox topLeftBoundgingBox = null;
                                    OCRBoundingBox bottomRightBoundgingBox = null;

                                    // Get first cordinate from first element
                                    var firstBoundingBox = section
                                                                .BoundingBox
                                                                .FirstOrDefault();

                                    if (firstBoundingBox != null)
                                    {
                                        var boundingBox = firstBoundingBox.FirstOrDefault();

                                        if (boundingBox != null)
                                        {
                                            topLeftBoundgingBox = new OCRBoundingBox() { x = boundingBox.X, y = boundingBox.Y };
                                            ocrBoundingBoxes.Add(topLeftBoundgingBox);
                                        }
                                    }

                                    // Get last cordinate from last element
                                    var lastBoundingBox = section
                                                            .BoundingBox
                                                            .LastOrDefault();

                                    if (lastBoundingBox != null)
                                    {
                                        var boundingBox = lastBoundingBox.LastOrDefault();

                                        if (boundingBox != null)
                                        {
                                            bottomRightBoundgingBox = new OCRBoundingBox() { x = boundingBox.X, y = boundingBox.Y };
                                            ocrBoundingBoxes.Add(bottomRightBoundgingBox);
                                        }
                                    }
                                }

                                var nlpHighlight = new NlpHighlight()
                                {
                                    PageNumber      = pageNumber,
                                    Text            = section.Name,
                                    Status          = section.Status,
                                    DocumentPageId  = documentPageId,
                                    BoundingBoxes   = ocrBoundingBoxes
                                };

                                nlpHighlights.Add(nlpHighlight);
                            }
                        }

                    }

                    documentPageNlpMatches = new DocumentPageNlpMatches()
                    {
                        NlpMatches = nlpHighlights
                    };
                }
            }

            return documentPageNlpMatches;
        }

        /// <summary>
        /// Returns list of NlpHighlights related to memnber information based on chase id
        /// </summary>
        /// <param name="chaseId"></param>
        /// <returns></returns>
        public async Task<DocumentPageNlpMatches> GetMemberHighlightsAsync(int chaseId)
        {
            DocumentPageNlpMatches documentPageNlpMatches = null;
            List<NlpHighlight> nlpHighlights = new List<NlpHighlight>();

            // Get the Response JSON from NlpRequest table based on ChaseID
            var nlpRequest = await GetNlpRequestDataAsync(chaseId, (int)NlpRequestStatus.Completed);

            if (nlpRequest != null && !string.IsNullOrEmpty(nlpRequest.ResponseDetail))
            {
                var ezDIResponse = JsonConvert.DeserializeObject<RiskNlpSubmissionResponse>(nlpRequest.ResponseDetail);

                if (ezDIResponse != null && ezDIResponse.MemberDetails != null && ezDIResponse.MemberDetails.Any())
                {
                    var members = ezDIResponse
                                        .MemberDetails
                                        .Where(item => item.MemberName != null);

                    if (members != null && members.Any())
                    {
                        foreach (var member in members)
                        {
                            List<OCRBoundingBox> ocrBoundingBoxes = new List<OCRBoundingBox>();

                            foreach (var boundingBox in member.BoundingBox)
                            {
                                foreach (var b in boundingBox)
                                {
                                    ocrBoundingBoxes.Add(new OCRBoundingBox() { x = b.X, y = b.Y });
                                }
                            }

                            var nlpHighlight = new NlpHighlight()
                            {
                                PageNumber      = member.PageNumber,
                                Text            = member.MemberName,
                                DocumentPageId  = member.DocumentPageId,
                                BoundingBoxes   = ocrBoundingBoxes
                            };

                            nlpHighlights.Add(nlpHighlight);
                        }
                    }
                }

                documentPageNlpMatches = new DocumentPageNlpMatches()
                {
                    NlpMatches = nlpHighlights
                };
            }

            return documentPageNlpMatches;
        }

        /// <summary>
        /// Generates the Print screen PDF for the Coding Module
        /// </summary>
        /// <param name="codingPrintScreenBinaryData"></param>
        /// <param name="auditPackageItemId"></param>
        /// <param name="chaseId"></param>
        /// <param name="callerUserId"></param>
        public async Task GeneratePrintScreenDocumentForClinicalModuleAsync(byte[] codingPrintScreenBinaryData, int chaseId, int auditPackageItemId, int callerUserId)
        {
            string bucket = _configuration
                                        .GetSection("Keys")
                                        .GetSection("S3EnvironmentBucket:Value").Value.Trim();

            string printScreenFilepathS3FileLocation = _configuration
                                .GetSection("Keys")
                                .GetSection("S3FileLocationForClinicalPrintScreen:Value").Value.Trim();

            string tempPngFileLocation = _configuration
                                .GetSection("Keys")
                                .GetSection("PngToPdfConversion:PngFolder").Value.Trim();

            var packageDetails = _auditService.GetPageSelectionEntriesAsync(auditPackageItemId, callerUserId).Result;

            string codingfileKeyPng = $"{printScreenFilepathS3FileLocation}/CodingOnly/{packageDetails.ProjectId}_{packageDetails.ChaseId}_{packageDetails.AuditPackageItemId}.png";

            var uploadedcodingPng = _s3Service.UploadFile(new MemoryStream(codingPrintScreenBinaryData), bucket, codingfileKeyPng);

            if (uploadedcodingPng)
            {
                var tmPngFilepath = $"{tempPngFileLocation}{packageDetails.AuditPackageItemId}.png";
                _s3Service.DownloadFile(bucket, codingfileKeyPng, tmPngFilepath);

                var result = ConvertPngToPdf(tmPngFilepath, chaseId, callerUserId);

                string codingfileKeyPdf = $"{printScreenFilepathS3FileLocation}/CodingOnly/{packageDetails.ProjectId}_{packageDetails.ChaseId}_{packageDetails.AuditPackageItemId}.pdf";
                
                var uploadedcodingPdf = _s3Service.UploadFile(new MemoryStream(result), bucket, codingfileKeyPdf);

                // Delete temp png input file from server temp folder
                if ((System.IO.File.Exists(tmPngFilepath)))
                {
                    System.IO.File.Delete(tmPngFilepath);
                }


                if (uploadedcodingPdf)
                {
                    AuditPageEntries auditPageEntries = new AuditPageEntries
                    {
                        AuditPackageItemId = packageDetails.AuditPackageItemId,
                        ChaseId = packageDetails.ChaseId,
                        ProjectId = packageDetails.ProjectId,
                        DataEntryPrintingUrl = $"{bucket}/{codingfileKeyPdf}",
                        IsDataEntryPrinting = packageDetails.IsDataEntryPrinting,
                        NumeratorId = packageDetails.NumeratorId,
                    };

                    await _auditService.UpdateAuditPackageItemAsync(auditPageEntries, callerUserId);

                }
            }
        }

        private byte[] ConvertPngToPdf(string tempPngFileLocation, int chaseId, int callerUserId)
        {
            IronPdf.License.LicenseKey = IronPdfLicenseKey;

            IronPdf.HtmlToPdf Renderer = new IronPdf.HtmlToPdf();

            string headerText = GenerateHeaderHtmlAsync(chaseId, callerUserId).Result;

            Renderer.PrintOptions.Header = new SimpleHeaderFooter
            {
                CenterText = headerText,
                DrawDividerLine = true,
                Spacing = 10
            };

            Renderer.PrintOptions.Footer = new HtmlHeaderFooter()
            {
                Height = 15,
                HtmlFragment = "<center><i><span style='font-size: 8px'>{page} of {total-pages}</span><i></center>",
                DrawDividerLine = true
            };

            Renderer.PrintOptions.DPI = 800;
            Renderer.PrintOptions.PaperSize = IronPdf.PdfPrintOptions.PdfPaperSize.Letter;
            Renderer.PrintOptions.MarginBottom = 10;
            Renderer.PrintOptions.MarginLeft = 10;
            Renderer.PrintOptions.MarginRight = 10;
            Renderer.PrintOptions.JpegQuality = 100;
            Renderer.PrintOptions.Zoom = 100;
            Renderer.PrintOptions.SetCustomPaperSizeInInches(10, 15);

            byte[] convertedPdf = Renderer.RenderHtmlAsPdf($"<body><html><img src='{tempPngFileLocation}' style='margin-left: 200px'></body></html>").BinaryData;

            return convertedPdf;
        }


        private async Task<string> GenerateHeaderHtmlAsync(int chaseId, int callerUserId)
        {
            var chaseData = _chaseRepository.GetChaseDetailGenericAsync
                                (chaseId: chaseId,
                                 callerUserId: callerUserId).Result;

            string printScreen_html = $"Member Name: {chaseData.MemberFirstName} {chaseData.MemberLastName} | DOB: {chaseData.MemberDateOfBirth.ToShortDateString()} |  Age: {chaseData.MemberAge.ToString()} | Sex: {chaseData.MemberGender.ToString()} | Measure: {chaseData.MeasureCode}" + Environment.NewLine
                                            + $"Project: {chaseData.ProjectName}  |  Chase ID: {chaseData.ChaseId.ToString()} | Client Chase Key: {chaseData.ChaseSourceAliasId} | Client Member ID: {chaseData.MemberSourceAliasId}";

            return printScreen_html;

        }

        /// <summary>
        /// Returns Chase Audit Log
        /// </summary>
        /// <param name="chaseId"></param>
        /// <param name="callerUserId"></param>
        /// <returns></returns>
        public async Task<ChaseCodingAudit> GetChaseAuditLogAsync(int chaseId, int callerUserId)
        {
            ChaseCodingAudit ChaseCodingAudit = new ChaseCodingAudit();
            ChaseCodingAudit = await _chaseRepository.GetChaseAuditLogAsync(chaseId, callerUserId);

            return ChaseCodingAudit;
        }

        /// <summary>
        /// Returns number of pages across all member chases
        /// </summary>
        /// <param name="chaseId"></param>
        /// <param name="callerUserId"></param>
        public async Task<int> GetTotalMemberChasePagesAsync(int chaseId)
        {
            var result =  await _chaseDocumentRepository.GetChaseDocumentsAsync(chaseId, 1, null);

            return  result.ToList().Count > 0
                ? result.ToList().FirstOrDefault().ChaseDocumentPageCount
                : 0;
        }

        /// <summary>
        /// Set HccDiscrepency and EncounterFound value
        /// </summary>
        private void SetHccDiscrepencyAndEncounterFoundValue(ChaseQuerySearchCriteria model)
        {
            switch (model.HccDiscrepency)
            {
                case "Yes":
                    model.HccDiscrepency = "1";
                    break;
                case "No":
                    model.HccDiscrepency = "0";
                    break;
                case "All":
                    model.HccDiscrepency = null;
                    break;

                default:
                    break;
            }
            switch (model.EncounterFound)
            {
                case "Yes":
                    model.EncounterFound = "1";
                    break;
                case "No":
                    model.EncounterFound = "0";
                    break;
                case "All":
                    model.EncounterFound = null;
                    break;

                default:
                    break;
            }
        }
    }
}
