import { HttpClient } from "@angular/common/http";
import { Inject, Injectable } from "@angular/core";
import { BehaviorSubject, Observable } from "rxjs";
import { map } from "rxjs/operators";
import { AutomapperService } from "../../../../core/automapper/automapper.service";
import { BASE_API_URL } from "../../../../core/environment.tokens";
import { ListItem } from "../../../../shared/list/list-item";
import { MemberCentricChase } from "../../../../shared/membercentric-doc-attachment/membercentric-chase.model";
import { obsolete } from "../../../../utilities/decorators/obsolete.decorator";
import { ChaseDetail } from "./chase-detail.model";

@Injectable()
export class ChaseDetailService {
  chaseDetailsChange = new BehaviorSubject<ListItem[]>([]);


  constructor(
    @Inject(BASE_API_URL) private readonly baseApiUrl: string,
    private http: HttpClient,
    private automapper: AutomapperService
  ) { }


  @obsolete("getChaseDetail") getSummaryItems(chaseId: number): Observable<ListItem[]> {
    const url = `${this.baseApiUrl}Chase?chaseId=${chaseId}`;

    return this.http.get(url).pipe(
      map(this.automapper.curryMany("default", "ListItem"))
    );
  }

  getChaseDetail(chaseId: number): Observable<ChaseDetail> {
    const url = `${this.baseApiUrl}chase/detail?chaseId=${chaseId}`;
    return this.http.get(url).pipe(
      map(this.automapper.curry("default", "ChaseDetail"))
    );
  }
}
