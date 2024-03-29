import { Injectable } from '@angular/core';
import { Response } from '@angular/http';
import 'rxjs/add/operator/map';
import { Observable } from 'rxjs/Observable';
import { LoginService } from './login.service';
import * as Constants from './constants';
import { Patient } from './patient';

@Injectable()
export class PatientService {

	constructor(public loginService: LoginService) {

	}

	search(searchText: string, pageNumber: number, pageSize: number): Observable<PagedListResult<Patient>> {
		return this.loginService.AuthGet(Constants.API_URL +  `/patients?pageNumber=${pageNumber}&pageSize=${pageSize}&search=${searchText}`)
			.map(this.extractData);
	}

	findOne(id: string): Observable<Patient> {
		return this.loginService.AuthGet(Constants.API_URL + '/patients/' + id)
			.map(this.extractData);
	}

	saveOrUpdate(patient: Patient): Observable<string> {
		if(patient.id != null){
			return this.loginService.AuthPut(Constants.API_URL + '/patients/' + patient.id, patient)
				.map(this.extractData);
		} else {
			return this.loginService.AuthPost(Constants.API_URL + '/patients', patient)
				.map(this.extractData);
		}
	}

	delete(id: string): Observable<void> {
		return this.loginService.AuthDelete(Constants.API_URL + '/patients/' + id)
			.map(this.extractData);
	}

	private extractData(res: Response) {
		try{
	      	const body = res.json();
		    return body.data || body;
		} catch(ex) {
			return res.text();
		}
	}

}

export class PagedListResult<T> {
	result: T[];
	totalCount: number;
	totalPages: number;
}
