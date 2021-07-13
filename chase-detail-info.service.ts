import { HttpClient } from "@angular/common/http";
import { Inject, Injectable } from "@angular/core";
import { ActivatedRoute } from "@angular/router";
import { Observable } from "rxjs";
import { map } from "rxjs/operators";
import { AutomapperService } from "../../../../../core/automapper/automapper.service";
import { BASE_API_URL } from "../../../../../core/environment.tokens";
import { CommentItem } from "../../../../../shared/comments/comment-item/comment-item";
import { TimelineItem } from "../../../../../shared/timeline/timeline-item.model";
import { NumberHelper } from "../../../../../utilities/contracts/number-helper";
import { GapCompliance } from "./gap-compliance.model";

@Injectable({
  providedIn: "root",
})
export class ChaseDetailInfoService {

  constructor(
    @Inject(BASE_API_URL) private readonly baseApiUrl: string,
    private http: HttpClient,
    private automapper: AutomapperService,
    private route: ActivatedRoute
  ) { }

  getChaseGdFromPath(): string {
    return this.route.snapshot.parent.params.chaseGd;
  }

  getComments(): Observable<CommentItem[]> {
    // TODO: put into the new service
    const url = `${this.baseApiUrl}Chase/Comment?chaseId=${this.getChaseGdFromPath()}&isOnlyLatest=true`;

    return this.http.get(url).pipe(
      map(this.automapper.curryMany("default", "CommentItem"))
    );
  }

  getTimelineItems(chaseId?: number): Observable<TimelineItem[]> {
    const url = (NumberHelper.isGreaterThan(chaseId, 0)) ? `${this.baseApiUrl}timeline/chaseTimelineActivity?chaseId=${chaseId}`
      : `${this.baseApiUrl}timeline/GetChaseTimeline?chaseId=${this.getChaseGdFromPath()}&maxRecords=5`;

    return this.http.get(url).pipe(
      map(this.automapper.curryMany("default", "TimelineItem"))
    );
  }

  getGapCompliance(chaseId: number): Observable<GapCompliance[]> {
    const url = `${this.baseApiUrl}gap/compliance?chaseId=${chaseId}`;
    return this.http.get(url).pipe(
      map(this.automapper.curryMany("default", "GapCompliance"))
    );
  }
}
