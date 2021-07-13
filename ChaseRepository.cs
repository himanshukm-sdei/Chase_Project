using Dapper;
using HDVI.Core.MRCS.Common;
using HDVI.Core.MRCS.DataContract.Catalytic;
using HDVI.Core.MRCS.DataContract.Chase;
using HDVI.Core.MRCS.DataContract.Chase.ChaseData;
using HDVI.Core.MRCS.DataContract.CommentItem;
using HDVI.Core.MRCS.DataContract.Database;
using HDVI.Core.MRCS.DataContract.Enums;
using HDVI.Core.MRCS.DataContract.EzDI;
using HDVI.Core.MRCS.DataContract.Member;
using HDVI.Core.MRCS.DataContract.Provider;
using HDVI.Core.MRCS.DataContract.Reporting;
using HDVI.Core.MRCS.DataContract.Retrieval;
using HDVI.Core.MRCS.DataContract.Tags;
using HDVI.Core.MRCS.DataContract.Workflow;
using HDVI.Core.MRCS.RepositoryContract.Chase;
using HDVI.Core.MRCS.RepositoryContract.Comment;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace HDVI.Core.MRCS.Repository.Chase
{
    /// <summary>
    ///  Repository to work with Chase from database.
    /// </summary>
    public class ChaseRepository : BaseRepository, IChaseRepository
    {
        private readonly ICommentRepository _commentRepository;

        public ChaseRepository(IConfiguration configuration, ICommentRepository commentRepository) : base(configuration)
        {
            _commentRepository = commentRepository;

            //TODO: Delete it after testing
            //IEnumerable<SqlColumnMapping> ChaseQueryItem = new List<SqlColumnMapping>()
            //{
            //    new SqlColumnMapping(){ Source = "ChaseID", Target = "ChaseId"},
            //    new SqlColumnMapping(){ Source = "ChaseSourceAliasID", Target = "ChaseSourceAliasId"},
            //    new SqlColumnMapping(){ Source = "LastCoder", Target = "LastCoderUserName"},
            //    new SqlColumnMapping(){ Source = "ChasePendID", Target = "ChasePendId"},
            //    new SqlColumnMapping(){ Source = "PendStatusID", Target = "PendStatusId"},
            //    new SqlColumnMapping(){ Source = "ParentChaseID", Target = "ParentChaseId"},
            //    new SqlColumnMapping(){ Source = "MemberID", Target = "MemberId"}
            //};
            //RegisterColumnMappings<ChaseQueryItem>(ChaseQueryItem);

            //IEnumerable<SqlColumnMapping> measureAttributeItem = new List<SqlColumnMapping>()
            //{
            //    new SqlColumnMapping(){ Source = "EntityTypeID", Target = "EntityTypeId"},
            //    new SqlColumnMapping(){ Source = "AttributeID", Target = "AttributeId"},
            //    new SqlColumnMapping(){ Source = "DataType", Target = "AttributeDataType"},

            //};
            //RegisterColumnMappings<MeasureAttributeData>(measureAttributeItem);

            //IEnumerable<SqlColumnMapping> chaseNlpDataColumnMappings = new List<SqlColumnMapping>()
            //{
            //    new SqlColumnMapping(){ Source = "Numerators", Target = "NumeratorsAsXml"},
            //    new SqlColumnMapping(){ Source = "ChaseID", Target = "ChaseId"},
            //     new SqlColumnMapping(){ Source = "CatalyticChaseNlpChaseDataID", Target = "ChaseNlpDataId"}
            //};

            //RegisterColumnMappings<ChaseNlpData>(chaseNlpDataColumnMappings);
        }


        /// <summary>
        /// Returns chase detail based on chase id.
        /// </summary>
        /// <param name="chaseId"></param>
        /// <param name="callerUserId"></param>
        /// <param name="workflowStatus"></param>
        /// <param name="projectId"></param>
        public async Task<spMR50_ChaseDetail_sel> GetChaseDetailGenericAsync(
            int chaseId, 
            int callerUserId, 
            string workflowStatus = null, 
            int? projectId = null
        ) {
            var query = "spMR50_ChaseDetail_sel";

            DynamicParameters parameter = new DynamicParameters();
            parameter.Add("@ChaseID", chaseId, DbType.Int32, ParameterDirection.Input);
            parameter.Add("@ProjectID", projectId, DbType.Int32, ParameterDirection.Input);
            parameter.Add("@WorkflowStatusName", workflowStatus, DbType.String, ParameterDirection.Input);
            parameter.Add("@CallerUserID", callerUserId, DbType.Int16, ParameterDirection.Input);
            parameter.Add("@Debug", false, DbType.Boolean, ParameterDirection.Input);

            var result = await GetFirstOrDefaultAsync<spMR50_ChaseDetail_sel>(query, parameter, commandType: CommandType.StoredProcedure);

            MustBe.Available(result, new BrokenRuleException($"There are no chase details for chaseId '{chaseId}'."));
            MustBe.Assert(result.ChaseId > 0, new BrokenRuleException($"There are no chase details for chaseId '{chaseId}'."));

            return result ?? new spMR50_ChaseDetail_sel();
        }

        /// <summary>
        /// Returns chase detail based on chase id.
        /// </summary>
        /// <param name="chaseId"></param>
        /// <param name="callerUserId"></param>
        public async Task<spMR50_ChaseDetail_sel> GetChaseDetailLookupAsync(int chaseId,int callerUserId)
        {
            var query = "spMR50_ChaseLookup_sel";

            DynamicParameters parameter = new DynamicParameters();
            parameter.Add("@ChaseID", chaseId, DbType.Int32, ParameterDirection.Input);
            parameter.Add("@CallerUserID", callerUserId, DbType.Int16, ParameterDirection.Input);
            parameter.Add("@Debug", false, DbType.Boolean, ParameterDirection.Input);

            var result = await GetFirstOrDefaultAsync<spMR50_ChaseDetail_sel>(query, parameter, commandType: CommandType.StoredProcedure);

            MustBe.Available(result, new BrokenRuleException($"There are no chase details for chaseId '{chaseId}'."));
            MustBe.Assert(result.ChaseId > 0, new BrokenRuleException($"There are no chase details for chaseId '{chaseId}'."));

            return result ?? new spMR50_ChaseDetail_sel();
        }

        /// <summary>
        /// Returns Archived Chase Detail
        /// </summary>
        /// <param name="chaseId"></param>
        /// <param name="callerUserId"></param>
        /// <returns></returns>
        public async Task<ChaseDetailArchive> GetChaseArchiveGenericAsync(int chaseId, int callerUserId)
        {
            var query = "spArch50_ChaseDetail_sel";

            DynamicParameters parameter = new DynamicParameters();
            parameter.Add("@ChaseID", chaseId, DbType.Int32, ParameterDirection.Input);
            parameter.Add("@CallerUserID", callerUserId, DbType.Int16, ParameterDirection.Input);

            var result = await GetFirstOrDefaultAsync<ChaseDetailArchive>(query, parameter, commandType: CommandType.StoredProcedure, databaseName: DatabaseName.Archive50);

            return result ?? new ChaseDetailArchive();
        }

        /// <summary>
        /// Returns chase details based on chase id.
        /// </summary>
        /// <param name="chaseId"></param>
        /// <param name="projectId"></param>
        /// <param name="workflowStatus"></param>
        /// <param name="callerUserId"></param>
        /// <returns></returns>
        [Obsolete("Use GetChaseDetailAsync instead.")]
        public async Task<ChaseDetailSummary> GetChaseDetailByIdAsync(int chaseId, int? projectId, string workflowStatus, int callerUserId)
        {
            // TODO: Use GetChaseDetailAsync to remove duplicate code. We will need to add IChaseDetail to the ChaseDetailSummary.
            ChaseDetailSummary result = null;

            var query = "spMR50_ChaseDetail_sel";

            // use anonymous method for parameter
            //result = QueryFirstOrDefault<Address>(sql, new { addressId = addressId }, commandType: CommandType.StoredProcedure);
            //return result;

            // use dynamic parameter
            DynamicParameters parameter = new DynamicParameters();
            parameter.Add("@ChaseID", chaseId, DbType.Int32, ParameterDirection.Input);
            parameter.Add("@ProjectID", projectId, DbType.Int32, ParameterDirection.Input);
            parameter.Add("@WorkflowStatusName", workflowStatus, DbType.String, ParameterDirection.Input);
            parameter.Add("@CallerUserID", callerUserId, DbType.Int16, ParameterDirection.Input);
            parameter.Add("@Debug", false, DbType.Boolean, ParameterDirection.Input);

            result = await GetFirstOrDefaultAsync<ChaseDetailSummary>(query, parameter, commandType: CommandType.StoredProcedure);
            
            MustBe.Available(result, new BrokenRuleException($"There are no chase details for chaseId '{chaseId}'."));
            MustBe.Assert(result.MeasureID > 0, new BrokenRuleException($"There are no chase details for chaseId '{chaseId}'."));

            return result;
        }

        /// <summary>
        /// Returns chase details based on chase id.
        /// </summary>
        /// <param name="chaseId"></param>
        /// <param name="callerUserId"></param>
        public async Task<ChaseDetail> GetChaseDetailAsync(int chaseId, int callerUserId)
        {
            var sp = await GetChaseDetailGenericAsync(chaseId, callerUserId);
            var chaseDetail = new ChaseDetail(sp);
            return chaseDetail;
        }

        /// <summary>
        /// Returns Archived Chase Detail
        /// </summary>
        /// <param name="chaseId"></param>
        /// <param name="callerUserId"></param>
        public async Task<ChaseArchive> GetChaseArchiveAsync(int chaseId, int callerUserId)
        {
            var sp = await GetChaseArchiveGenericAsync(chaseId, callerUserId);
            var chaseArchive = new ChaseArchive(sp);
            return chaseArchive;
        }

        /// <summary>
        /// Returns HEDIS chase data based on chase id.
        /// </summary>
        /// <param name="chaseId"></param>
        /// <param name="callerUserId"></param>
        /// <param name="workflowStatus"></param>
        public async Task<HedisChaseData> GetHedisChaseDetailAsync(int chaseId, int callerUserId, string workflowStatus = null)
        {
            var sp = await GetChaseDetailGenericAsync(chaseId, callerUserId, workflowStatus);
            var hedisChaseData = new HedisChaseData(sp);
            return hedisChaseData;
        }

        /// <summary>
        /// Returns RISK chase data based on chase id.
        /// </summary>
        /// <param name="chaseId"></param>
        /// <param name="callerUserId"></param>
        /// <param name="workflowStatus"></param>
        public async Task<RiskChaseData> GetRiskChaseDetailAsync(int chaseId, int callerUserId, string workflowStatus = null)
        {
            var sp = await GetChaseDetailGenericAsync(chaseId, callerUserId, workflowStatus);
            var riskChaseData = new RiskChaseData(sp);
            return riskChaseData;
        }

        /// <summary>
        /// Returns chase data based on chaseid.
        /// </summary>
        /// <param name="chaseId"></param>
        /// <param name="callerUserId"></param>
        /// <param name="workflowStatus"></param>
        public async Task<HedisChaseData> UpdateChaseDataAsync(int chaseId, int entityTypeId, int entityId, string xml, int callerUserId)
        {
            var query = "spMR50_Entity_mod";

            DynamicParameters parameter = new DynamicParameters();
            parameter.Add("@EntityID", entityId, DbType.Int32, ParameterDirection.Input);
            parameter.Add("@ChaseID", chaseId, DbType.Int32, ParameterDirection.Input);
            parameter.Add("@ParentEntityID", null, DbType.Int32, ParameterDirection.Input);
            parameter.Add("@EntityTypeID", entityTypeId, DbType.Int32, ParameterDirection.Input);
            parameter.Add("@DocumentSourceID", null, DbType.Int32, ParameterDirection.Input);
            parameter.Add("@ServiceProviderID", null, DbType.Int32, ParameterDirection.Input);
            parameter.Add("@SourceAliasID", null, DbType.Int32, ParameterDirection.Input);
            parameter.Add("@EntityAttributeXml", xml, DbType.Int32, ParameterDirection.Input);
            parameter.Add("@CallerUserID", callerUserId, DbType.Int16, ParameterDirection.Input);
            parameter.Add("@Debug", false, DbType.Boolean, ParameterDirection.Input);

            await AddAsync(query, parameter, commandType: CommandType.StoredProcedure);
            // TODO: Change to get Risk or Hedis data?
            var result = await this.GetHedisChaseDetailAsync(chaseId, callerUserId);
            return result;
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
            var query = "spMR50_Entity_mod";
            var result = 0;

            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("@EntityID", entityId, DbType.Int32, ParameterDirection.InputOutput);
            parameters.Add("@ChaseID", chaseId, DbType.Int32, ParameterDirection.Input);
            parameters.Add("@ParentEntityID", null, DbType.Int32, ParameterDirection.Input);
            parameters.Add("@EntityTypeID", null, DbType.Int32, ParameterDirection.Input);
            parameters.Add("@DocumentSourceID", null, DbType.Int32, ParameterDirection.Input);
            parameters.Add("@ServiceProviderID", null, DbType.Int32, ParameterDirection.Input);
            parameters.Add("@SourceAliasID", null, DbType.Int32, ParameterDirection.Input);
            parameters.Add("@EntityAttributeData", "", DbType.Xml, ParameterDirection.Input);
            parameters.Add("@CallerUserID", callerUserId, DbType.Int16, ParameterDirection.Input);
            parameters.Add("@Debug", false, DbType.Boolean, ParameterDirection.Input);

            await base.AddAsync(query, parameters, commandType: CommandType.StoredProcedure);
            result = parameters.Get<int>("EntityID");

            await LogHelper
                    .LogAsWarningAsync(string.Format("Deleted data for EventId: {0} and ChaseId: {1}", result, chaseId));

        }

        /// <summary>
        /// Gets the chase Comments from DB tables.
        /// </summary>
        /// <param name="chaseId"></param>
        /// <returns>Collection of Comments to Comment Service</returns>
        public async Task<IEnumerable<CommentItem>> GetChaseCommentsAsync(int chaseId)
        {
            return await _commentRepository.GetCommentsAsync((int)CommentType.Chase, chaseId);
        }

        /// <summary>
        /// Saves the Chase comments to DB.
        /// </summary>
        /// <param name="commentItem"></param>
        public async Task AddChaseCommentAsync(CommentItem commentItem)
        {
            await _commentRepository.AddCommentAsync(commentItem);
        }

        /// <summary>
        /// Returns a list of Chase data based on values in chaseSearchCriteria
        /// </summary>
        [Obsolete("Use GetChasesQueryListAsync")]
        public async Task<IEnumerable<ChaseSearchResult>> ChaseSearchAsync(ChaseSearchCriteria chaseSearchCriteria)
        {
            IEnumerable<ChaseSearchResult> result = null;

            var query = "spMR50_Chase_sel";

            // use dynamic parameter
            DynamicParameters parameter = new DynamicParameters();

            // Chase search specific parameters
            parameter.Add("@ProjectID", chaseSearchCriteria.ProjectId, DbType.Int32, ParameterDirection.Input);
            parameter.Add("@ChaseID", chaseSearchCriteria.ChaseId, DbType.Int32, ParameterDirection.Input);
            parameter.Add("@FunctionalRoleID", chaseSearchCriteria.FunctionalRoleId, DbType.Int16, ParameterDirection.Input);
            parameter.Add("@MasterDocumentSourceID", chaseSearchCriteria.DocumentSourceId, DbType.Int32, ParameterDirection.Input);
            parameter.Add("@MeasureID", chaseSearchCriteria.MeasureId, DbType.Int32, ParameterDirection.Input);
            parameter.Add("@SourceAliasID", chaseSearchCriteria.SourceAliasId, DbType.String, ParameterDirection.Input);
            parameter.Add("@WorkflowStatusID", chaseSearchCriteria.WorkflowStatusId, DbType.Int16, ParameterDirection.Input);
            parameter.Add("@MemberID", chaseSearchCriteria.MemberId, DbType.Int32, ParameterDirection.Input);
            parameter.Add("@MemberName", chaseSearchCriteria.MemberName, DbType.String, ParameterDirection.Input, 100);
            parameter.Add("@MemberDateOfBirth", (!String.IsNullOrEmpty(chaseSearchCriteria.MemberDateOfBirth)) ? DateTime.Parse(chaseSearchCriteria.MemberDateOfBirth) : (DateTime?)null, DbType.Date, ParameterDirection.Input);
            parameter.Add("@ProviderName", chaseSearchCriteria.ProviderName, DbType.String, ParameterDirection.Input, 100);
            parameter.Add("@DocumentSourceContactValue", chaseSearchCriteria.ProviderPhone, DbType.String, ParameterDirection.Input, 50);
            parameter.Add("@FullTextSearchCondition", chaseSearchCriteria.FullTextSearch, DbType.String, ParameterDirection.Input);
            parameter.Add("@AssignedToUserID", chaseSearchCriteria.AssignedToUserId, DbType.Int16, ParameterDirection.Input);
            parameter.Add("@ClientID", chaseSearchCriteria.ClientId, DbType.Int16, ParameterDirection.Input);
            parameter.Add("@MemberFirstName", chaseSearchCriteria.MemberFirstName, DbType.String, ParameterDirection.Input, 100);
            parameter.Add("@MemberLastName", chaseSearchCriteria.MemberLastName, DbType.String, ParameterDirection.Input, 100);
            parameter.Add("@MemberID", chaseSearchCriteria.MemberId, DbType.Int32, ParameterDirection.Input);
            parameter.Add("@DocumentSourceTypeId", chaseSearchCriteria.DocumentSourceTypeId, DbType.Int32, ParameterDirection.Input);
            parameter.Add("@SampleComplianceCodeList", chaseSearchCriteria.SampleComplianceCodesAsXml, DbType.Xml, ParameterDirection.Input);                       

            // Common Parameters
            parameter.Add("@SortOrder", chaseSearchCriteria.SortField, DbType.String, ParameterDirection.Input, 20);
            parameter.Add("@SortDirection", chaseSearchCriteria.SortDirection, DbType.String, ParameterDirection.Input, 20);
            parameter.Add("@StartRecord", chaseSearchCriteria.StartRecord, DbType.Int32, ParameterDirection.Input);
            parameter.Add("@EndRecord", chaseSearchCriteria.EndRecord, DbType.Int32, ParameterDirection.Input);
            parameter.Add("@CallerUserID", chaseSearchCriteria.CallerUserId, DbType.Int16, ParameterDirection.Input);
            parameter.Add("@PendCode", chaseSearchCriteria.PendCode, DbType.String, ParameterDirection.Input);
            parameter.Add("@Debug", false, DbType.Boolean, ParameterDirection.Input);
            parameter.Add("@PursuitChaseFilter", chaseSearchCriteria.Pursuit, DbType.String, ParameterDirection.Input);
            parameter.Add("@TagID", chaseSearchCriteria.TagId, DbType.Int32, ParameterDirection.Input);
            parameter.Add("@TagList", chaseSearchCriteria.TagIdsAsXml, DbType.Xml, ParameterDirection.Input);

            result = await GetAsync<ChaseSearchResult>(query, parameter, commandType: CommandType.StoredProcedure);

            if (result != null)
            {
                result.ToList().ForEach(x =>
                {
                    x.PursuitChases = this.GetPursuitChases(x.PursuitChaseData);
                    x.TagsText = this.GetTagsText(x.TagData);
                });
            }

            return result;
        }

        /// <summary>
        /// Get a chase query list.
        /// </summary>
        public async Task<IEnumerable<ChaseQueryItem>> GetChasesQueryListAsync(ChaseQuerySearchCriteria model)
        {
            IEnumerable<ChaseQueryItem> result = null;

            var query = "spMR50_Chase_sel";
            var commandTimeout = 90;

            var databaseName = DatabaseName.MRCS50ReadOnly;
            if (model.UseTransactionalDatabase)
            {
                databaseName = DatabaseName.MRCS50;
            }

            var parameter = new DynamicParameters();
            parameter.Add("@ChaseID", model.ChaseId, DbType.Int32, ParameterDirection.Input);
            parameter.Add("@MasterDocumentSourceID", model.AddressId, DbType.Int32, ParameterDirection.Input);
            parameter.Add("@ClientID", model.ClientId, DbType.Int16, ParameterDirection.Input);
            parameter.Add("@ProjectList", model.Projects, DbType.Xml, ParameterDirection.Input);
            parameter.Add("@MeasureList", model.MeasuresCodes, DbType.Xml, ParameterDirection.Input);
            parameter.Add("@SourceAliasID", model.ChaseIdAndClientChaseKey, DbType.String, ParameterDirection.Input);
            parameter.Add("@ReportingStatusList", model.Statuses, DbType.Xml, ParameterDirection.Input);
            parameter.Add("@FunctionalRoleList", model.FunctionalRoleIdsXml, DbType.Xml, ParameterDirection.Input);
            parameter.Add("@AddressGrouping", model.AddressGroupId, DbType.String, ParameterDirection.Input, 50);
            parameter.Add("@AssignedToUserID", model.AssignedToUserId, DbType.Int16, ParameterDirection.Input);
            parameter.Add("@MemberName", model.MemberName, DbType.String, ParameterDirection.Input, 100); // TODO: Remove line?
            parameter.Add("@MemberID", model.MemberId, DbType.Int32, ParameterDirection.Input); // TODO: Remove line?
            parameter.Add("@MemberList", model.MemberIdsXml, DbType.Xml, ParameterDirection.Input); // TODO: Remove line?
            parameter.Add("@MemberFirstName", model.MemberFirstName, DbType.String, ParameterDirection.Input, 100);
            parameter.Add("@MemberLastName", model.MemberLastName, DbType.String, ParameterDirection.Input, 100);
            parameter.Add("@MemberDateOfBirth", model.MemberDob, DbType.Date, ParameterDirection.Input);
            parameter.Add("@MemberSourceAliasID", model.MemberSourceAliasID, DbType.String, ParameterDirection.Input, 50);
            parameter.Add("@PendCodeList", model.PendCodesXml, DbType.Xml, ParameterDirection.Input);
            parameter.Add("@PendStatusList", model.PendsStatusXml, DbType.Xml, ParameterDirection.Input);
            parameter.Add("@ComplianceCodeList", model.ComplianceCodesXml, DbType.Xml, ParameterDirection.Input);
            parameter.Add("@SampleComplianceCodeList", model.SampleComplianceCodesAsXml, DbType.Xml, ParameterDirection.Input); 
            parameter.Add("@Product", model.Product, DbType.String, ParameterDirection.Input, 50);
            parameter.Add("@LastCoderUserID", model.LastCoderUserId, DbType.Int16, ParameterDirection.Input);
            parameter.Add("@AssignedToFilter", model.AssignedToFilter, DbType.String, ParameterDirection.Input);
            parameter.Add("@AssignedFilter", model.StatisticsFilter, DbType.String, ParameterDirection.Input);
            parameter.Add("@SortOrder", model.SortField, DbType.String, ParameterDirection.Input, 30);
            parameter.Add("@SortDirection", model.SortDirection, DbType.String, ParameterDirection.Input, 20);
            parameter.Add("@StartRecord", model.StartRecord, DbType.Int32, ParameterDirection.Input);
            parameter.Add("@EndRecord", model.EndRecord, DbType.Int32, ParameterDirection.Input);
            parameter.Add("@CallerUserID", model.CallerUserId, DbType.Int16, ParameterDirection.Input);
            parameter.Add("@DateAssigned", model.DateAssigned, DbType.Date, ParameterDirection.Input);
            parameter.Add("@EncounterFound", model.EncounterFound, DbType.String, ParameterDirection.Input, 20);
            parameter.Add("@EncounterIsFaceToFace", model.EncounterIsFaceToFace, DbType.String, ParameterDirection.Input, 20);
            parameter.Add("@HccDiscrepency", model.HccDiscrepency, DbType.String, ParameterDirection.Input, 20);
            parameter.Add("@ProviderName", model.ServiceProviders, DbType.String, ParameterDirection.Input);
            parameter.Add("@PursuitChaseFilter", model.Pursuit, DbType.String, ParameterDirection.Input);
            parameter.Add("@CompletedByUserID", model.CompletedByUserId, DbType.Int16, ParameterDirection.Input);
            parameter.Add("@ChaseList", model.ChaseIdsAsXml, DbType.Xml, ParameterDirection.Input);
            parameter.Add("@ChartAcquisitionDate", model.ChartAcquired, DbType.Date, ParameterDirection.Input);
            parameter.Add("@TagID", model.TagId, DbType.Int16, ParameterDirection.Input);
            parameter.Add("@TagList", model.TagIdsAsXml, DbType.Xml, ParameterDirection.Input);
            parameter.Add("@TagsSearchOperator", model.TagSearchOperator, DbType.String, ParameterDirection.Input, 3);

            result = await GetAsync<ChaseQueryItem>(query, parameter, commandTimeout, commandType: CommandType.StoredProcedure, databaseName: databaseName);
            result.ToList().ForEach(x =>
            {
                var documentSource = x.DocumentSourceData != null ? XmlHelper.GetFromXml<DocumentSourceItemXml>(x.DocumentSourceData).DocumentSource.FirstOrDefault() : null;
                if (documentSource != null)
                {
                    x.DocumentSourceId = documentSource.DocumentSourceId;
                    x.MasterDocumentSourceId = documentSource.MasterDocumentSourceId;
                    x.DocumentSourceTypeId = documentSource.DocumentSourceTypeId;
                }
                else
                {
                    x.DocumentSourceId = 0;
                    x.MasterDocumentSourceId = 0; // TODO: Remove this line because this property can be null.
                }

                x.MemberName = x.MemberFirstName + " " + x.MemberLastName;
                x.ChaseCount = x.RecordCount;
                x.RecordCount = x.RecordCount >= 1000 ? 1000 : x.RecordCount;
                x.ServiceProviders = this.GetServiceProviders(x.ServiceProviderData);
                x.TagsText = this.GetTagsText(x.TagData);
                x.PursuitChases = this.GetPursuitChases(x.PursuitChaseData);
            });

            return result;
        }

        /// <summary>
        /// Returns Archived Chase Query List
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<ChaseQueryItem>> GetArchiveChaseQueryListAsync(MemberSearchCriteria model)
        {
            IEnumerable<ChaseQueryItem> result = null;

            var query = "spMbr50_Chase_sel";

            var parameter = new DynamicParameters();
            parameter.Add("@MemberID", model.MemberId, DbType.Int32, ParameterDirection.Input);
            parameter.Add("@DataSet", model.DataSet, DbType.String, ParameterDirection.Input, 50);
            parameter.Add("@CallerUserID", model.CallerUserId, DbType.Int16, ParameterDirection.Input);

            result = await GetAsync<ChaseQueryItem>(query, parameter, commandType: CommandType.StoredProcedure, databaseName: DatabaseName.Member50);
    
            return result;
        }

        /// <summary>
        /// Return a list of chase WorkflowStatus.
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<WorkflowStatusItem>> GetWorkflowStatusesAsync()
        {
            var query = "spMR50_WorkflowStatus_sel";
            IEnumerable<WorkflowStatusItem> result = null;
            result = await GetAsync<WorkflowStatusItem>(query, null, commandType: CommandType.StoredProcedure);
            return result;
        }

        /// <summary>
        /// Return a list of chase ReportingStatus.
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<ReportingStatusItem>> GetReportingStatusesAsync()
        {
            var query = "spMR50_ReportingStatus_sel";

            IEnumerable<ReportingStatusItem> result = null;

            result = await GetAsync<ReportingStatusItem>(query, null, commandType: CommandType.StoredProcedure);

            return result;
        }

        /// <summary>
        /// UnAssign Chase from a User.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> UnAssignChasesAsync(ChaseUnassignModel unassignModel)
        {

            var query = "spMR50_ChaseTeam_mod";

            DynamicParameters parameter = new DynamicParameters();
            parameter.Add("@ChaseList", XmlHelper.GetAsXml(unassignModel.ChaseListXML), DbType.Xml, ParameterDirection.Input);
            parameter.Add("@MasterDocumentSourceID", unassignModel.MasterDocumentSourceId, DbType.Int32, ParameterDirection.Input);
            parameter.Add("@UserID", unassignModel.UserId, DbType.Int16, ParameterDirection.Input);
            parameter.Add("@FunctionalRoleID", unassignModel.FunctionalRoleId, DbType.Int16, ParameterDirection.Input);
            parameter.Add("@Action", "Delete", DbType.String, ParameterDirection.Input);
            parameter.Add("@CallerUserID", unassignModel.CallerUserId, DbType.Int16, ParameterDirection.Input);
            await UpdateAsync(query, parameter, commandType: CommandType.StoredProcedure);

            return true;
        }


        /// <summary>
        /// Reopens chase in Closed Status
        /// </summary>
        /// <param name="chaseId"></param>
        /// <param name="callerUserId"></param>
        /// <returns></returns>
        public async Task<bool> ReopenAsync(int chaseId, int callerUserId)
        {

            // TODO: Waiting for DB Team to implement

            throw new BrokenRuleException("Reopen Chase functionality is not implemented yet");


        }

        /// <summary>
        /// Move chases to another address id.
        /// </summary>       
        public async Task<bool> ChaseMoveToAnotherAIDAsync(ChaseMoveModel model)
        {
            var query = "spMR50_ChaseRequest_mod";

            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("@ChaseList", model.ChaseIds, DbType.Xml, ParameterDirection.Input);
            parameters.Add("@DestinationMasterDocumentSourceID", model.ToAddressId, DbType.String, ParameterDirection.Input);
            parameters.Add("@RequestType", "chase move", DbType.String, ParameterDirection.Input);
            parameters.Add("@Notes", model.Notes, DbType.String, ParameterDirection.Input);
            parameters.Add("@Status", model.Status, DbType.String, ParameterDirection.Input);
            parameters.Add("@CallerUserID", model.LoginUserId, DbType.Int16, ParameterDirection.Input);
            parameters.Add("@ReassignChaseToOriginalUsers", model.IsChaseAssign, DbType.Boolean, ParameterDirection.Input);
            parameters.Add("@Debug", false, DbType.Boolean, ParameterDirection.Input);

            await AddAsync(query, parameters, commandType: CommandType.StoredProcedure);

            return true;
        }

        /// <summary>
        /// Get address assigned users.
        /// </summary>       
        /// <returns></returns>
        public async Task<IEnumerable<AddressesQueueSearchResult>> GetAssignedUserAsync(ChaseMoveModel model)
        {
            var query = "spMR50_MasterDocumentSourceQueue_sel";

            DynamicParameters parameter = new DynamicParameters();
            parameter.Add("@MasterDocumentSourceID", model.ToAddressId, DbType.String, ParameterDirection.Input);
            parameter.Add("@CallerUserID", model.LoginUserId, DbType.Int16, ParameterDirection.Input);

            await AddAsync(query, parameter, commandType: CommandType.StoredProcedure);

            var result = await GetAsync<AddressesQueueSearchResult>(query, parameter, commandType: CommandType.StoredProcedure);

            return result;
        }

        private string GetServiceProviders(string data)
        {
            if (String.IsNullOrWhiteSpace(data))
            {
                return "";
            }

            var xml = XmlHelper.GetFromXml<ServiceProvidersXml>(data);
            if (xml == null || xml.ServiceProviders == null || !xml.ServiceProviders.Any())
            {
                return "";
            }

            var serviceProviders = xml.ServiceProviders.Select(a => a.Name);
            var serviceProvidersCsv = String.Join(", ", serviceProviders);
            return serviceProvidersCsv;
        }

        /// <summary>
        /// Returns list of chase pursuits in csv format.
        /// </summary>
        private string GetPursuitChases(string data)
        {
            if (string.IsNullOrWhiteSpace(data))
            {
                return "";
            }

            var xml = XmlHelper.GetFromXml<PursuitChasesXml>(data);
            if (xml == null || xml.ChasePursuits == null || !xml.ChasePursuits.Any())
            {
                return "";
            }

            var chasePursuits = xml.ChasePursuits.Select(a => a.ChaseId);
            var chasePursuitsCsv = String.Join(", ", chasePursuits);
            return chasePursuitsCsv;
        }

        /// <summary>
        /// Returns a list of tags in csv format.
        /// </summary>
        private string GetTagsText(string data)
        {
            if (string.IsNullOrWhiteSpace(data))
            {
                return "";
            }

            var xml = XmlHelper.GetFromXml<TagsTextXml>(data);
            if (xml == null || xml.TagsText == null || !xml.TagsText.Any())
            {
                return "";
            }

            var tagsText = xml.TagsText.Select(a => a.TagText);
            var tagsTextCsv = String.Join(", ", tagsText);
            return tagsTextCsv;
        }

        /// <summary>
        /// Returns chase list recordset based on chase Id.
        /// </summary>
        /// <param name="chaseId"></param>
        /// <param name="callerUserId"></param>
        public async Task<IEnumerable<ChaseSearchResult>> GetChaseListByIdAsync(int chaseId, int callerUserId)
        {
            var query = "spMR50_ChaseDocumentCopy_sel";

            // use dynamic parameter
            DynamicParameters parameter = new DynamicParameters();
            parameter.Add("@ChaseID", chaseId, DbType.Int32, ParameterDirection.Input);
            parameter.Add("@CallerUserID", callerUserId, DbType.Int16, ParameterDirection.Input);
            parameter.Add("@Debug", false, DbType.Boolean, ParameterDirection.Input);

            var result = await GetAsync<ChaseSearchResult>(query, parameter, commandType: CommandType.StoredProcedure);

            return result;
        }
        /// <summary>
        /// Returns Member Chase Query List
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<ChaseSearchResult>> GetMemberChaseQueryListAsync(int memberId, string dataSet, int callerUserId)
        {
            IEnumerable<ChaseSearchResult> result = null;

            var query = "spMbr50_Chase_sel";

            var parameter = new DynamicParameters();
            parameter.Add("@MemberID", memberId, DbType.Int32, ParameterDirection.Input);
            parameter.Add("@DataSet", dataSet, DbType.String, ParameterDirection.Input, 50);
            parameter.Add("@CallerUserID", callerUserId, DbType.Int16, ParameterDirection.Input);

            result = await GetAsync<ChaseSearchResult>(query, parameter, commandType: CommandType.StoredProcedure, databaseName: DatabaseName.Member50);
            return result;
        }

        /// <summary>
        /// To Copy Chart to Another Chase.
        /// </summary>
        /// <param name="callerUserId"></param>
        /// <param name="sourceChaseId"></param>
        /// <param name="TargetChaseId"></param>
        public async Task<bool> CopyChartToAnotherChaseAsync(int sourceChaseId, int targetChaseId, int callerUserId)
        {
            var query = "spMR50_ChaseDocumentCopy_ins ";

            // use dynamic parameter
            DynamicParameters parameter = new DynamicParameters();
            parameter.Add("@CallerUserID", callerUserId, DbType.Int16, ParameterDirection.Input);
            parameter.Add("@SourceChaseID ", sourceChaseId, DbType.Int32, ParameterDirection.Input);
            parameter.Add("@TargetChaseID ", targetChaseId, DbType.Int32, ParameterDirection.Input);

            await AddAsync(query, parameter, commandType: CommandType.StoredProcedure);

            return true;
        }

        /// <summary>
        /// Creates a chase
        /// </summary>
        /// <param name="chaseCreateRequest"></param>
        /// <param name="callerUserId"></param>
        /// <returns>The id of newly created chase and member-centric parent ChaseId</returns>
        public async Task<ChaseCreateResponse> CreateChaseAsync(ChaseCreateRequest chaseCreateRequest, int callerUserId)
        {
            int chaseId = 0;
            int? parentChaseId = 0;

            // Get data from database
            var query = "spMR50_Chase_ins";

            // set parameters
            // required parameters
            DynamicParameters parameter = new DynamicParameters();
            parameter.Add("@CallerUserID", callerUserId, DbType.Int16, ParameterDirection.Input);
            parameter.Add("@SourceAliasID", chaseCreateRequest.ChaseKey, DbType.String, ParameterDirection.Input);
            parameter.Add("@ProjectID", chaseCreateRequest.ProjectId, DbType.Int32, ParameterDirection.Input);
            parameter.Add("@MeasureCode", chaseCreateRequest.MeasureCode, DbType.String, ParameterDirection.Input);
            parameter.Add("@ChaseID", 0, DbType.Int32, ParameterDirection.Output);
            parameter.Add("@MrrChaseID", 0, DbType.Int32, ParameterDirection.Output);
            parameter.Add("@Product", chaseCreateRequest.ProductName, DbType.String, ParameterDirection.Input);
            parameter.Add("@Action", chaseCreateRequest.Action, dbType: DbType.Boolean, direction: ParameterDirection.Input);
            parameter.Add("@SampleSourceAliasID", chaseCreateRequest.SampleId, DbType.String, ParameterDirection.Input);
            parameter.Add("@Sequence", chaseCreateRequest.Sequence, DbType.Int32, ParameterDirection.Input);
            parameter.Add("@LineOfBusiness", chaseCreateRequest.LineOfBusiness, DbType.String, ParameterDirection.Input);

            // optional parameters
            if (chaseCreateRequest.ProviderId.HasValue && chaseCreateRequest.RiskChaseProviderData.Count < 1)
            {
                parameter.Add("@ServiceProviderID", chaseCreateRequest.ProviderId, DbType.Int32, ParameterDirection.Input);
            }

            if (chaseCreateRequest.MemberId.HasValue)
            {
                parameter.Add("@MemberID", chaseCreateRequest.MemberId, DbType.Int32, ParameterDirection.Input);
            }

            if (chaseCreateRequest.AddressId.HasValue)
            {
                parameter.Add("@MasterDocumentSourceID", chaseCreateRequest.AddressId, DbType.Int32, ParameterDirection.Input);
            }

            if(chaseCreateRequest.ChaseMemberData != null)
            {
                var chaseMembers = new ChaseMembers()
                {
                    Members = new List<ChaseMemberData>(){ chaseCreateRequest.ChaseMemberData }
                };

                var chaseMemberDataAsXml = XmlHelper.GetAsXml<ChaseMembers>(chaseMembers);

                if (!string.IsNullOrEmpty(chaseMemberDataAsXml))
                {
                    parameter.Add("@MemberData", chaseMemberDataAsXml, DbType.Xml, ParameterDirection.Input);
                }
            }

            if (chaseCreateRequest.ChaseAddressData != null)
            {
                var chaseAddresses = new ChaseAddress()
                {
                    Addresses = new List<ChaseAddressData>() { chaseCreateRequest.ChaseAddressData }
                };

                var chaseAddressDataAsXml = XmlHelper.GetAsXml<ChaseAddress>(chaseAddresses);

                if (!string.IsNullOrEmpty(chaseAddressDataAsXml))
                {
                    parameter.Add("@DocumentSourceData", chaseAddressDataAsXml, DbType.Xml, ParameterDirection.Input);
                }
            }

            if (chaseCreateRequest.ChaseProviderData != null)
            {
                var chaseProviders = new ChaseProviders()
                {
                    Providers = new List<ChaseProviderData>() { chaseCreateRequest.ChaseProviderData }
                };

                var chaseProviderDataAsXml = XmlHelper.GetAsXml<ChaseProviders>(chaseProviders);

                if (!string.IsNullOrEmpty(chaseProviderDataAsXml))
                {
                    parameter.Add("@ProviderData", chaseProviderDataAsXml, DbType.Xml, ParameterDirection.Input);
                }
            }

            if (chaseCreateRequest.RiskChaseProviderData != null)
            {
                var chaseProviders = new ChaseProviders()
                {
                    Providers = chaseCreateRequest.RiskChaseProviderData
                };

                var chaseProviderDataAsXml = XmlHelper.GetAsXml<ChaseProviders>(chaseProviders);

                if (!string.IsNullOrEmpty(chaseProviderDataAsXml))
                {
                    parameter.Add("@ProviderData", chaseProviderDataAsXml, DbType.Xml, ParameterDirection.Input);
                }
            }

            if (chaseCreateRequest.MeasureAttributeData != null)
            {
                var entityDataAsXml = GetEntityDataAsXml(chaseCreateRequest.MeasureAttributeData);

                if (!string.IsNullOrEmpty(entityDataAsXml))
                {
                    parameter.Add("@EntityData", entityDataAsXml, DbType.Xml, ParameterDirection.Input);
                }
            }
            if (chaseCreateRequest.Encounters != null)
            {
                var entityDataAsXml = GetRiskChaseDataAsXml(chaseCreateRequest.Encounters, chaseCreateRequest.ChaseProviderData);

                if (!string.IsNullOrEmpty(entityDataAsXml))
                {
                    parameter.Add("@EntityData", entityDataAsXml, DbType.Xml, ParameterDirection.Input);
                }
            }
            if (chaseCreateRequest.NumeratorList != null)
            {
                var numerators = chaseCreateRequest.NumeratorList.Numerator.Select(x => x.Extra.Code);
                var numeratorCodes = String.Join("; ", numerators);
                numeratorCodes = !string.IsNullOrEmpty(numeratorCodes) ? numeratorCodes + ";" : numeratorCodes;
                parameter.Add("@NumeratorList", numeratorCodes, DbType.String, ParameterDirection.Input);
            }

            await GetAsync<ChaseCreateResponse>(query, parameter, commandType: CommandType.StoredProcedure);

            chaseId = (int)parameter.Get<int>("@ChaseID");
            var mrrChaseId = parameter.Get<int?>("@MrrChaseID");
            parentChaseId = mrrChaseId == null ? 0 : mrrChaseId;
            var response = new ChaseCreateResponse()
            {
                ChaseID = chaseId,
                MrrChaseID = parentChaseId
            };
            return response;
        }


        /// <summary>
        /// Validate chase
        /// </summary>
        /// <param name="chaseCreateRequest"></param>
        /// <param name="callerUserId"></param>
        public async Task<string> ValidateNewChaseAsync(ChaseCreateRequest chaseCreateRequest, int callerUserId)
        {
            var response = string.Empty;
            var query = "spMR50_Chase_ins";
            DynamicParameters parameter = new DynamicParameters();
            parameter.Add("@CallerUserID", callerUserId, DbType.Int16, ParameterDirection.Input);
            parameter.Add("@SourceAliasID", chaseCreateRequest.ChaseKey, DbType.String, ParameterDirection.Input);
            parameter.Add("@ProjectID", chaseCreateRequest.ProjectId, DbType.Int32, ParameterDirection.Input);
            parameter.Add("@MeasureCode", chaseCreateRequest.MeasureCode, DbType.String, ParameterDirection.Input);
            parameter.Add("@LineOfBusiness", chaseCreateRequest.LineOfBusiness, DbType.String, ParameterDirection.Input);
            parameter.Add("@Action", chaseCreateRequest.Action, dbType: DbType.Boolean, direction: ParameterDirection.Input);

            if (chaseCreateRequest.ChaseMemberData != null)
            {
                var chaseMembers = new ChaseMembers()
                {
                    Members = new List<ChaseMemberData>() { chaseCreateRequest.ChaseMemberData }
                };

                var chaseMemberDataAsXml = XmlHelper.GetAsXml<ChaseMembers>(chaseMembers);

                if (!string.IsNullOrEmpty(chaseMemberDataAsXml))
                {
                    parameter.Add("@MemberData", chaseMemberDataAsXml, DbType.Xml, ParameterDirection.Input);
                }
            }

            if (chaseCreateRequest.ChaseAddressData != null)
            {
                var chaseAddresses = new ChaseAddress()
                {
                    Addresses = new List<ChaseAddressData>() { chaseCreateRequest.ChaseAddressData }
                };

                var chaseAddressDataAsXml = XmlHelper.GetAsXml<ChaseAddress>(chaseAddresses);

                if (!string.IsNullOrEmpty(chaseAddressDataAsXml))
                {
                    parameter.Add("@DocumentSourceData", chaseAddressDataAsXml, DbType.Xml, ParameterDirection.Input);
                }
            }

            if (chaseCreateRequest.ChaseProviderData != null)
            {
                var chaseProviders = new ChaseProviders()
                {
                    Providers = new List<ChaseProviderData>() { chaseCreateRequest.ChaseProviderData }
                };

                var chaseProviderDataAsXml = XmlHelper.GetAsXml<ChaseProviders>(chaseProviders);

                if (!string.IsNullOrEmpty(chaseProviderDataAsXml))
                {
                    parameter.Add("@ProviderData", chaseProviderDataAsXml, DbType.Xml, ParameterDirection.Input);
                }
            }

            if (chaseCreateRequest.Encounters != null)
            {
                var entityDataAsXml = GetRiskChaseDataAsXml(chaseCreateRequest.Encounters, chaseCreateRequest.ChaseProviderData);

                if (!string.IsNullOrEmpty(entityDataAsXml))
                {
                    parameter.Add("@EntityData", entityDataAsXml, DbType.Xml, ParameterDirection.Input);
                }
            }

            try
            {
                await GetAsync<Task>(query, parameter, commandType: CommandType.StoredProcedure);
            }
            catch (BrokenRuleException ex)
            {
                return ex.Message;
            }

            return response;
        }

        /// <summary>
        /// Returns entity data as xml, which is used for measure
        /// </summary>
        /// <param name="measureAttributeData"></param>
        private string GetEntityDataAsXml(List<MeasureAttributeData> measureAttributeData)
        {
            //Logic to support multiple entities.
            var chaseMeasureEntities = new List<ChaseMeasureEntity>();
            foreach (var data in measureAttributeData)
            {
                chaseMeasureEntities.Add(
                new ChaseMeasureEntity()
                {
                    EntityTypeId = data.EntityTypeId,
                    EntityAttributes = new List<EntityAttribute>()
                            {   new EntityAttribute() {
                                     ChaseMeasureEntityAttribute =
                                      new ChaseMeasureEntityAttribute()
                                          {
                                            AttributeId       = data.AttributeId,
                                            AttributeValue    = data.AttributeValue,
                                            SourceTypeId      = (int)SourceType.Client
                                          }
                                }
                            }

                });

            }
            var chaseMeasureAttributes = new ChaseMeasureAttributes()
            {
                ChaseMeasureEntities = chaseMeasureEntities
            };
            var entityDataAsXml = XmlHelper.GetAsXml<ChaseMeasureAttributes>(chaseMeasureAttributes);

            return entityDataAsXml;
        }

        /// <summary>
        /// Gets chase details by chase key and projectid
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="chaseKey"></param>
        /// <param name="callerUserId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<ChaseQueryItem>> GetChaseDetailByChaseKeyAsync(int projectId, string chaseKey, int callerUserId)
        {
            IEnumerable<ChaseQueryItem> result = null;

            var query = "spMR50_Chase_sel";
            var commandTimeout = 90;

            var parameter = new DynamicParameters();
            parameter.Add("@ProjectID", projectId, DbType.Int32, ParameterDirection.Input);
            parameter.Add("@SourceAliasID", chaseKey, DbType.String, ParameterDirection.Input);
            parameter.Add("@CallerUserID", callerUserId, DbType.Int16, ParameterDirection.Input);

            result = await GetAsync<ChaseQueryItem>(query, parameter, commandTimeout, commandType: CommandType.StoredProcedure);
            
            return result;
        }

        /// <summary>
        /// Gets list of attribute data related to a measure
        /// </summary>
        /// <param name="measureId"></param>
        /// <param name="callerUserId"></param>
        /// <returns>IEnumerable<MeasureAttributeData></returns>
        public async Task<IEnumerable<MeasureAttributeData>> GetAttributeDataByMeasureAsync(int measureId, int callerUserId)
        {
            IEnumerable<MeasureAttributeData> result = null;

            var query = "spMR50_EntityType_sel";

            var parameter = new DynamicParameters();
            parameter.Add("@MeasureID", measureId, DbType.Int32, ParameterDirection.Input);
            parameter.Add("@CallerUserID", callerUserId, DbType.Int16, ParameterDirection.Input);

            result = await GetAsync<MeasureAttributeData>(query, parameter, commandType: CommandType.StoredProcedure);

            return result;
        }

        /// <summary>
        /// Saves the Nlp data from catalytic process
        /// </summary>
        /// <param name="chaseNlpData"></param>
        /// <param name="calleUserId"></param>
        /// <returns></returns>
        public async Task SaveChaseNlpDataAsync(ChaseNlpData chaseNlpData, int calleUserId)
        {
            var query = "spLog50_CatalyticChaseNlpChaseData_mod";

            // Set parameters
            DynamicParameters parameter = new DynamicParameters();
            //parameter.Add("@CatalyticChaseNlpChaseDataID", chaseNlpData.ChaseNlpDataId, DbType.Int32, ParameterDirection.Input);
            parameter.Add("@ChaseID", chaseNlpData.ChaseId, DbType.Int32, ParameterDirection.Input);
            parameter.Add("@OrganizationID", chaseNlpData.OrganizationId, DbType.Int32, ParameterDirection.Input);
            parameter.Add("@CreateUserID", calleUserId, DbType.Int32, ParameterDirection.Input);
            parameter.Add("@TotalMatch", chaseNlpData.TotalMatch, DbType.Int16, ParameterDirection.Input);
            parameter.Add("@TotalNoMatch", chaseNlpData.TotalNoMatch, DbType.Int16, ParameterDirection.Input);          
            parameter.Add("@TotalPartialMatch", chaseNlpData.TotalPartialMatch, DbType.Int16, ParameterDirection.Input);
            parameter.Add("@SystemResultsReviewed", chaseNlpData.SystemResultsReviewed, DbType.Boolean, ParameterDirection.Input);
            parameter.Add("@Notes", chaseNlpData.Notes, DbType.String, ParameterDirection.Input);
            parameter.Add("@NumeratorsList", chaseNlpData.NumeratorsAsXml, DbType.Xml, ParameterDirection.Input);

            await GetFirstOrDefaultAsync<bool>(query, parameter, commandType: CommandType.StoredProcedure, databaseName: DatabaseName.Logging50);
        }

        /// <summary>
        /// Gets reults of nlp processing of a chase
        /// </summary>
        /// <param name="chaseId"></param>
        /// <returns></returns>
        public async Task<ChaseNlpData> GetChaseNlpDataAsync(int chaseId)
        {
            ChaseNlpData chaseNlpData = null;

            var query = "spLog50_CatalyticChaseNlpChaseData_sel";

            // Set parameters
            DynamicParameters parameter = new DynamicParameters();
            parameter.Add("@ChaseID", chaseId, DbType.Int32, ParameterDirection.Input);

            chaseNlpData = await GetFirstOrDefaultAsync<ChaseNlpData>(query, parameter, commandType: CommandType.StoredProcedure, databaseName: DatabaseName.Logging50);

            return chaseNlpData;
        }

        /// <summary>
        /// Gets list of event data for a chase
        /// </summary>
        /// <param name="chaseId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<ChaseNlpEventData>> GetChaseNlpEventDataAsync(int chaseId)
        {
            IEnumerable< ChaseNlpEventData> chaseNlpEventData = null;

            var query = "spMR50_ChaseEntityType_sel";

            // Set parameters
            DynamicParameters parameter = new DynamicParameters();
            parameter.Add("@ChaseID", chaseId, DbType.Int32, ParameterDirection.Input);

            chaseNlpEventData = await GetAsync<ChaseNlpEventData>(query, parameter, commandType: CommandType.StoredProcedure);

            return chaseNlpEventData;
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
            var query = "spMR50_ChaseDocumentGetPages_sel";

            DynamicParameters parameter = new DynamicParameters();
            parameter.Add("@ChaseId", chaseId, dbType: DbType.Int32, direction: ParameterDirection.Input);
            parameter.Add("@documentTypeId", documentTypeId, dbType: DbType.Int32, direction: ParameterDirection.Input);
            parameter.Add("@begPage", begPage, dbType: DbType.Int32, direction: ParameterDirection.Input);
            parameter.Add("@endPage", endPage, dbType: DbType.Int32, direction: ParameterDirection.Input);

            var result = await GetAsync<ChaseDocumentPage>(query, parameter, commandType: CommandType.StoredProcedure);

            return result;
        }


        /// <summary>
        /// Returns entity data as xml, which is used for Encounter and Diagnosis
        /// </summary>
        /// <param name="riskChaseData"></param>
        private string GetRiskChaseDataAsXml(Encounters riskChaseData, ChaseProviderData providerData)
        {
            var riskChaseDataEntities = new List<RiskChaseEncounterEntity>();
            var encounters = riskChaseData.Encounter.ToList();
            foreach (var encounter in encounters)
            {
                string encounterTypeData = ((Newtonsoft.Json.Linq.JToken)encounter.EncounterType)?.Root?.ToString();
                SelectableInputData encounterType = new SelectableInputData();
                if (!string.IsNullOrEmpty(encounterTypeData))
                {
                    encounterType = JsonConvert.DeserializeObject<SelectableInputData>(encounterTypeData);
                }
                var encounterEntity = new RiskChaseEncounterEntity()
                {
                    EntityTypeId = encounter.EntityTypeData.EntityTypeId,
                    EntityTypeName = encounter.EntityTypeData.EntityTypeName,
                };
                encounterEntity.EntityAttributes = new RiskChaseEntityAttributes()
                {
                    RiskChaseEntityAttribute = new List<RiskChaseEntityAttribute>()
                    {
                        new RiskChaseEntityAttribute
                        {
                            AttributeId = encounter.claimAttributeData.AttributeId,
                            SourceTypeId = (int)SourceType.Client,
                            AttributeCode = encounter.claimAttributeData.AttributeCode,
                            AttributeValue = Convert.ToString(encounter.ClaimId)
                        },
                        new RiskChaseEntityAttribute
                        {

                            AttributeId = Convert.ToInt32(encounterType.Extra.AttributeId),
                            SourceTypeId = (int)SourceType.Client,
                            AttributeCode = encounterType.Extra.AttributeCode,
                            AttributeValue = encounterType.Extra.Value
                        },
                        new RiskChaseEntityAttribute
                        {
                            AttributeId = encounter.startDateAttributeData.AttributeId,
                            SourceTypeId = (int)SourceType.Client,
                            AttributeCode = encounter.startDateAttributeData.AttributeCode,
                            AttributeValue = encounter.EncounterServiceDateFrom == null ? "" : Convert.ToDateTime(encounter.EncounterServiceDateFrom).ToString("MM/dd/yyyy")
                        },
                         new RiskChaseEntityAttribute
                        {
                            AttributeId = encounter.endDateAttributeData.AttributeId,
                            SourceTypeId = (int)SourceType.Client,
                            AttributeCode = encounter.endDateAttributeData.AttributeCode,
                            AttributeValue = encounter.EncounterServiceDateThru == null ? "" : Convert.ToDateTime(encounter.EncounterServiceDateThru).ToString("MM/dd/yyyy")
                        },
                         new RiskChaseEntityAttribute
                        {
                            AttributeId = encounter.Provider.ProviderIdAttributeData.AttributeId,
                            SourceTypeId = (int)SourceType.Client,
                            AttributeCode = encounter.Provider.ProviderIdAttributeData.AttributeCode,
                            AttributeValue = !string.IsNullOrEmpty(encounter.Provider.ClientProviderId) ? encounter.Provider.ClientProviderId : encounter.Provider.SourceAliasId
                        },
                           new RiskChaseEntityAttribute
                        {
                            AttributeId = encounter.Provider.ProviderNameAttributeData.AttributeId,
                            SourceTypeId = (int)SourceType.Client,
                            AttributeCode = encounter.Provider.ProviderNameAttributeData.AttributeCode,
                            AttributeValue = encounter.Provider.FirstName + " " + encounter.Provider.LastName
                        },
                    }

                };

                encounterEntity.RiskChaseDiagnosisEntities = new List<RiskChaseEntity>();
                foreach (var diagnosis in encounter.Diagnosis)
                {
                    string diagnosisCodeData = ((Newtonsoft.Json.Linq.JToken)diagnosis.diagnosiscode)?.Root?.ToString();
                    SelectableInputData diagnosisCode = new SelectableInputData();
                    if (!string.IsNullOrEmpty(diagnosisCodeData))
                    {
                        diagnosisCode = JsonConvert.DeserializeObject<SelectableInputData>(diagnosisCodeData);
                    }
                    var diagnosisEntity = new RiskChaseEntity()
                    {
                        EntityTypeId = diagnosis.EntityTypeData.EntityTypeId,
                        EntityTypeName = diagnosis.EntityTypeData.EntityTypeName,
                    };
                    diagnosisEntity.EntityAttributes = new RiskChaseEntityAttributes()
                    {
                        RiskChaseEntityAttribute = new List<RiskChaseEntityAttribute>()
                        {
                            new RiskChaseEntityAttribute
                            {
                                AttributeId = diagnosis.diagnosisCodeAttributeData.AttributeId,
                                SourceTypeId = (int)SourceType.Client,
                                AttributeCode = diagnosis.diagnosisCodeAttributeData.AttributeCode,
                                AttributeValue = diagnosisCode.Value
                            },
                            new RiskChaseEntityAttribute
                            {
                                AttributeId = diagnosis.startDateAttributeData.AttributeId,
                                SourceTypeId = (int)SourceType.Client,
                                AttributeCode = diagnosis.startDateAttributeData.AttributeCode,
                                AttributeValue = diagnosis.diagnosisServiceDateFrom == null ? "" : Convert.ToDateTime(diagnosis.diagnosisServiceDateFrom).ToString("MM/dd/yyyy")
                            },
                            new RiskChaseEntityAttribute
                            {
                                AttributeId = diagnosis.endDateAttributeData.AttributeId,
                                SourceTypeId = (int)SourceType.Client,
                                AttributeCode = diagnosis.endDateAttributeData.AttributeCode,
                                AttributeValue = diagnosis.diagnosisServiceDateThru == null ? "" : Convert.ToDateTime(diagnosis.diagnosisServiceDateThru).ToString("MM/dd/yyyy")
                            },
                            new RiskChaseEntityAttribute
                            {
                                AttributeId = encounter.Provider.ProviderIdAttributeData.AttributeId,
                                SourceTypeId = (int)SourceType.Client,
                                AttributeCode = encounter.Provider.ProviderIdAttributeData.AttributeCode,
                                AttributeValue = !string.IsNullOrEmpty(encounter.Provider.ClientProviderId) ? encounter.Provider.ClientProviderId : encounter.Provider.SourceAliasId
                            },
                        }

                    };
                    encounterEntity.RiskChaseDiagnosisEntities.Add(diagnosisEntity);
                }
                riskChaseDataEntities.Add(encounterEntity);
            }

            var riskChaseAttributes = new RiskChaseAttributes()
            {
                RiskChaseEntities = riskChaseDataEntities
            };
            var entityRiskChaseDataAsXml = XmlHelper.GetAsXml<RiskChaseAttributes>(riskChaseAttributes);

            return entityRiskChaseDataAsXml;
        }

        /// <summary>
        /// Gets list of numerator data related to a measure
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="measureId"></param>
        /// <returns>IEnumerable<NumeratorData></returns>
        public async Task<IEnumerable<NumeratorData>> GetNumeratorListByMeasureAsync(int projectId, int measureId)
        {
            IEnumerable<NumeratorData> result = null;

            var query = "spMR50_Numerator_sel";

            var parameter = new DynamicParameters();
            parameter.Add("@ProjectID", projectId, DbType.Int16, ParameterDirection.Input);
            parameter.Add("@MeasureID", measureId, DbType.Int32, ParameterDirection.Input);
            result = await GetAsync<NumeratorData>(query, parameter, commandType: CommandType.StoredProcedure);

            return result;
        }


        /// <summary>
        /// Update Chase NLP Request Response Data 
        /// </summary>
        /// <param name="nlpRequestResponseData"></param>
        /// <returns></returns>
        public async Task UpdateChaseNLPRequestResponseDataAsync(NlpRequestResponseDataLog nlpRequestResponseDataLog)
        {
            var sql = "spRP50_NLPRequestResponseData_mod";

            // parameters
            DynamicParameters parameter = new DynamicParameters();
            parameter.Add("@ChaseID", nlpRequestResponseDataLog.ChaseId, DbType.Int32, ParameterDirection.Input);
            parameter.Add("@ProjectTypeId", nlpRequestResponseDataLog.ProjectTypeId, DbType.Int32, ParameterDirection.Input);
            parameter.Add("@ResponseDate", nlpRequestResponseDataLog.ResponseDate, DbType.DateTime, ParameterDirection.Input);
            parameter.Add("@ResponseData", nlpRequestResponseDataLog.ResponseData, DbType.String, ParameterDirection.Input);

            await AddAsync(sql, parameter, commandType: CommandType.StoredProcedure, databaseName: DatabaseName.Reporting50);
        }

        /// <summary>
        /// Gets record from NlpRequest table based on chase id
        /// </summary>
        /// <param name="chaseId"></param>
        /// <param name="requestStatusId"></param>
        /// <returns></returns>
        public async Task<NlpRequest> GetNlpRequestDataAsync(int chaseId, int requestStatusId)
        {
            NlpRequest result = null;
            var sql = "spNlp50_NlpRequest_sel";
            
            // parameters
            DynamicParameters parameter = new DynamicParameters();
            parameter.Add("@ChaseID", chaseId, DbType.Int32, ParameterDirection.Input);
            parameter.Add("@RequestStatusID", requestStatusId, DbType.Int32, ParameterDirection.Input);

            result = await GetFirstOrDefaultAsync<NlpRequest>(sql, parameter, commandType: CommandType.StoredProcedure, databaseName: DatabaseName.Nlp50);

            return result;
        }

        /// <summary>
        /// This API will return list of Chases.
        /// </summary>
        /// <param name="chaseSearchCriteria"></param>
        public async Task<IEnumerable<ChaseSearchResult>> ChaseTagSearchAsync(ChaseSearchCriteria chaseSearchCriteria)
        {
            var query = "spMR50_Chase_sel";

            DynamicParameters parameter = new DynamicParameters();
            parameter.Add("@TagID", chaseSearchCriteria.TagId, DbType.Int16, ParameterDirection.Input);
            parameter.Add("@TagList", chaseSearchCriteria.TagIdsAsXml, DbType.Xml, ParameterDirection.Input);
            parameter.Add("@TagsSearchOperator", chaseSearchCriteria.TagSearchOperator, DbType.String, ParameterDirection.Input, 3);
            parameter.Add("@SortOrder", chaseSearchCriteria.SortField, DbType.String, ParameterDirection.Input, 20);
            parameter.Add("@SortDirection", chaseSearchCriteria.SortDirection, DbType.String, ParameterDirection.Input, 20);
            parameter.Add("@StartRecord", chaseSearchCriteria.StartRecord, DbType.Int32, ParameterDirection.Input);
            parameter.Add("@EndRecord", chaseSearchCriteria.EndRecord, DbType.Int32, ParameterDirection.Input);
            parameter.Add("@CallerUserID", chaseSearchCriteria.CallerUserId, DbType.Int16, ParameterDirection.Input);

            IEnumerable<ChaseSearchResult> chaseSearchResult = await GetAsync<ChaseSearchResult>(query, parameter, commandType: CommandType.StoredProcedure);
            return chaseSearchResult;
        }

        /// <summary>
        /// Returns Chase Audit Log
        /// </summary>
        /// <param name="chaseId"></param>
        /// <param name="callerUserId"></param>
        public async Task<ChaseCodingAudit> GetChaseAuditLogAsync(int chaseId, int callerUserId)
        {
            var sp = await GetChaseAuditLogGenericAsync(chaseId, callerUserId);
            var ChaseCodingAudit = new ChaseCodingAudit(sp);
            return ChaseCodingAudit;
        }

        /// <summary>
        /// Returns Chase Audit Log
        /// </summary>
        /// <param name="chaseId"></param>
        /// <param name="callerUserId"></param>
        /// <returns></returns>
        public async Task<spMR50_ChaseCodingAudit_sel> GetChaseAuditLogGenericAsync(int chaseId, int callerUserId)
        {
            var query = "spMR50_ChaseCodingAudit_sel";

            DynamicParameters parameter = new DynamicParameters();
            parameter.Add("@ChaseID", chaseId, DbType.Int32, ParameterDirection.Input);
            parameter.Add("@CallerUserID", callerUserId, DbType.Int16, ParameterDirection.Input);
            var result = await GetFirstOrDefaultAsync<spMR50_ChaseCodingAudit_sel>(query, parameter, commandType: CommandType.StoredProcedure);

            return result ?? new spMR50_ChaseCodingAudit_sel();
        }
    }
}
