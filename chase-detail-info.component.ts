import { ChangeDetectionStrategy, ChangeDetectorRef, Component, ElementRef, OnDestroy, OnInit, ViewChild } from "@angular/core";
import { Router } from "@angular/router";
import { List } from "immutable";
import { Subject } from "rxjs";
import { takeUntil } from "rxjs/operators";
import { SubSink } from "subsink";
import { CommentItem } from "../../../../../shared/comments/comment-item/comment-item";
import { DocumentRequest } from "../../../../../shared/document/document-request.model";
import { DocumentRequestService } from "../../../../../shared/document/document-request.service";
import { ListItem } from "../../../../../shared/list/list-item";
import { CreatePendService } from "../../../../../shared/pend/create-pend.service";
import { TimelineItem } from "../../../../../shared/timeline/timeline-item.model";
import { ArrayHelper } from "../../../../../utilities/contracts/array-helper";
import { NumberHelper } from "../../../../../utilities/contracts/number-helper";
import { ProjectType } from "../../../project/project-type.enum";
import { ChaseDetailState } from "../chase-detail-state.model";
import { ChaseDetailStateService } from "../chase-detail-state.service";
import { ChaseDetailInfoService } from "./chase-detail-info.service";
import { GapCompliance } from "./gap-compliance.model";

@Component({
  selector: "member-chase-detail-info",
  templateUrl: "./chase-detail-info.component.html",
  styleUrls: ["./chase-detail-info.component.scss"],
  providers: [ChaseDetailInfoService],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ChaseDetailInfoComponent implements OnInit, OnDestroy {
  private sink = new SubSink();
  private unsubscribe = new Subject();
  private chaseId: string;
  chaseDetailState: ChaseDetailState;
  timelineItems = List<TimelineItem>();
  commentItems = List<CommentItem>();
  requiredDocuments: DocumentRequest[];
  gapCompliance: GapCompliance[];
  @ViewChild("containerBody", {static: true}) containerBody: ElementRef;

  constructor(
    private chaseDetailStateService: ChaseDetailStateService,
    private service: ChaseDetailInfoService,
    private serviceReset: CreatePendService,
    private documentRequestService: DocumentRequestService,
    private changeDetector: ChangeDetectorRef,
    private router: Router
  ) {
    this.chaseId = this.service.getChaseGdFromPath();
  }

  ngOnInit() {
    this.containerBody?.nativeElement?.offsetParent?.classList.add("container-body-wrap");
    this.sink.add(
      this.chaseDetailStateService.state.subscribe(state => {
        this.chaseDetailState = state;
        this.updateInfo();
        this.changeDetector.markForCheck();
      }),
      this.chaseDetailStateService.timelineItems.subscribe(timeline => {
        this.timelineItems = List(timeline);
        this.changeDetector.markForCheck();
      })
    );

    if (this.isHedis) {
      this.sink.add(
        this.service.getGapCompliance(Number(this.chaseId)).subscribe(result => {
          this.gapCompliance = result.map(item => {
            return item.chaseComplianceCode === "NC" ? {...item, status: "Open"} : {...item, status: "Closed"};
          });
        })
      );
    }

    this.serviceReset.reset
      .pipe(takeUntil(this.unsubscribe))
      .subscribe(id => this.updateInfo());
  }

  ngOnDestroy(): void {
    this.unsubscribe.next();
    this.unsubscribe.complete();
    this.sink.unsubscribe();
  }

  get summaryItems(): any {
    const list = [
      new ListItem({
        key: "Measure ID",
        value: this.chaseDetailState.measureCode,
      }),
      new ListItem({
        key: "Status",
        value: this.chaseDetailState.reportingStatusName,
      }),
      new ListItem({
        key: "Last Coded by",
        value: this.chaseDetailState.lastCoder,
      }),
      new ListItem({
        key: "Project",
        value: this.chaseDetailState.hasProject ? this.chaseDetailState.project.projectName : "",
      }),
      new ListItem({
        key: "Project ID",
        value: this.chaseDetailState.hasProject && this.chaseDetailState.project.hasProjectId ?
          this.chaseDetailState.project.projectId.toString() : "",
      }),
      new ListItem({
        key: "Client",
        value: this.chaseDetailState.hasProject ? this.chaseDetailState.project.clientName : "",
      }),
      new ListItem({
        key: "Product",
        value: this.chaseDetailState.product,
      }),
      new ListItem({
        key: "Assigned To",
        value: this.chaseDetailState.assignedToName,
      }),
      new ListItem({
        key: "Member ID",
        value: this.chaseDetailState.hasMember && this.chaseDetailState.member.hasMemberId ?
          this.chaseDetailState.member.memberId.toString() : "",
      }),
      new ListItem({
        key: "Client Chase Key",
        value: this.chaseDetailState.chaseSourceAliasId,
      }),
      new ListItem({
        key: "Address Id",
        value: NumberHelper.isGreaterThan(this.chaseDetailState.masterDocumentSourceId, 0)  ? this.chaseDetailState.masterDocumentSourceId.toString() : "",
        url: `/retrieval/addressdetail/${this.chaseDetailState.masterDocumentSourceId}`,
      }),
      new ListItem({
        key: "Pend Code",
        value: this.chaseDetailState.pendCode,
      }),
      new ListItem({
        key: "Gender",
        value: this.chaseDetailState.hasMember ? this.chaseDetailState.member.memberGender : "",
      }),
    ];

    if (this.hasParentChaseId) {
      list.push(
        new ListItem({
          key: "Parent Chase",
          value: this.chaseDetailState.parentChaseId.toString(),
          url: `/members/chase/${this.chaseDetailState.parentChaseId}`,
        })
      );
    }

    if (this.isHedis && this.chaseDetailState.hasGaps) {
      list.push(
        new ListItem({
          key: "Open Gaps",
          value: this.openGaps.toString(),
        }),
        new ListItem({
          key: "Closed Gaps",
          value: this.closedGaps.toString(),
        })
      );
    }

    return List(list);
  }

  get openGaps(): number {
    let gaps = 0;

    if (this.chaseDetailState.hasGaps && this.gapCompliance) {
      this.gapCompliance.forEach(item => {
        if (item.chaseComplianceCode === "NC") {
          gaps++;
        }
      });
    }

    return gaps;
  }

  get closedGaps(): number {
    let gaps = 0;

    if (this.chaseDetailState.hasGaps && this.gapCompliance) {
      this.gapCompliance.forEach(item => {
        if (item.chaseComplianceCode !== "NC") {
          gaps++;
        }
      });
    }

    return gaps;
  }

  get addressUrlOfChase(): string {
    return `/retrieval/addressdetail/${this.chaseDetailState.masterDocumentSourceId}`;
  }

  get documentRequests(): DocumentRequest[] {
    if (ArrayHelper.isAvailable(this.requiredDocuments)) {
      return this.requiredDocuments;
    } else {
      return [];
    }
  }

  get hasParentChaseId(): boolean {
    return NumberHelper.isGreaterThan(this.chaseDetailState.parentChaseId, 0);
  }

  get pursuitItems(): any {
    const items = this.chaseDetailState.pursuitData;

    return items;
  }

  get isHedis(): boolean {
    return this.chaseDetailState.projectTypeId === ProjectType.HEDIS;
  }

  updateInfo() {
    const documentRequestModel = new DocumentRequest({ chaseId: Number(this.chaseId)});
    this.documentRequestService.get(documentRequestModel).subscribe(data => {
      this.requiredDocuments = data;
    });

    this.service
      .getTimelineItems()
      .subscribe(items => this.timelineItems = this.assignAndNotify(items));

    this.service
      .getComments()
      .subscribe(items => this.commentItems = this.assignAndNotify(items));
  }

  showTimelineDetails() {
    this.router.navigateByUrl(`members/chase/${this.chaseDetailState.chaseId}/timeline`);
  }

  getStatusClass(status: string): string {
    return status === "Open" ? "open-compliance" : "closed-compliance";
  }

  trackByIndex(index, item) {
    return index;
  }

  private assignAndNotify<T>(data: T[]): List<T> {
    this.changeDetector.markForCheck();
    const dataList = List(data);

    return dataList;
  }
}
