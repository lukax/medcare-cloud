import {Component, OnInit} from '@angular/core';
import {Http} from '@angular/http';
import {ToastController, LoadingController, ActionSheetController, NavController} from 'ionic-angular';
import {Observable} from 'rxjs/Observable';
import * as Constants from '../../providers/constants';
import {Patient} from '../../providers/patient.service';
import {PatientFilePage} from '../patient-file/patient-file';
import {PatientService} from '../../providers/patient.service';

@Component({
  templateUrl: 'search.html'
})
export class SearchPage implements OnInit {
  patients: Patient[];
  pageSize: number = 25;
  pageNumber: number = 1;
  isLoading: boolean;
  searchPatientsObservable: Observable<void>;
  searchEvent: any;

  constructor(private http: Http,
              private toastCtrl: ToastController,
              private actionSheetCtrl: ActionSheetController,
              private loadingCtrl: LoadingController,
              private navCtrl: NavController,
              private patientSvc: PatientService) {


  }

  ngOnInit() {
    this.searchPatients({ target: { value: '' }});
  }

  searchPatients($searchEvent: any, $infinteScrollEvent?: any): void {
    this.searchEvent = $searchEvent;
    let searchText = $searchEvent.target.value || '';
    if($infinteScrollEvent){
      this.pageNumber++;
    } else {
      this.pageNumber = 1;
    }
    this.isLoading = true;
    this.patientSvc.search(searchText, this.pageNumber, this.pageSize)
      .subscribe((data) => {
        this.isLoading = false;
        let result = data.result;
        if($infinteScrollEvent){
          if(result.length > 0){
            this.patients.push(...result);
          } else {
            this.pageNumber --;
          }
        } else {
          this.patients = result;
        }
      }, (err) => {
        this.isLoading = false;
        if($infinteScrollEvent){
          this.pageNumber--;
        }
        this.toastCtrl.create({message: 'Oops... erro ao se comunicar com servidor', duration: 5000, showCloseButton: true, dismissOnPageChange: true, closeButtonText: 'OK'}).present();
      }, () => {
        if($infinteScrollEvent) {
          $infinteScrollEvent.complete();
        }
      });
  }

  getPatient(patient: Patient) {
    this.navCtrl.push(PatientFilePage, { patientId: patient.id });
  }

  presentLoading() {
    let loader = this.loadingCtrl.create({
      content: "Aguarde...",
      duration: 3000
    });
    loader.present();
  }

  patientAutocompleteListFormatter(data: Patient): string {
    let str = data.name;
    if (data.medicalInsurance) { str += ` (${data.medicalInsurance})`; };
    return str;
  }

  isEmptyPatients() {
    return this.patients != null && this.patients.length == 0;
  }

  doInfinite(infiniteScroll) {
    this.searchPatients(this.searchEvent, infiniteScroll);
  }
}

