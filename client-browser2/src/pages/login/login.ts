import {Component, OnInit} from '@angular/core';
import {AlertController, LoadingController, NavController} from 'ionic-angular';
import {LoginService} from '../../providers/login.service';
import {SearchPage} from '../../pages/search/search';
import {RegisterPage} from '../../pages/register/register';

@Component({
  templateUrl: 'login.html'
})
export class LoginPage implements OnInit {
  isLoading: boolean;
  username: string;
  password: string;

  constructor(private alertCtrl: AlertController,
              private loadingCtrl: LoadingController,
              private navCtrl: NavController,
              private loginService: LoginService) {


  }

  ngOnInit() {
      
  }

  login(){
    let loader = this.loadingCtrl.create();
    loader.present();
    this.loginService.login(this.username, this.password)
      .then(() => {
        loader.dismiss().then(() => {
          this.navCtrl.setRoot(SearchPage);
        });
      })
      .catch((err) => {
        loader.dismiss().then(() => {
          let alert = this.alertCtrl.create({
            title: 'Oops...',
            subTitle: 'Não foi possível fazer login. ' + (err.message || err.error || err.errorMessage || 'Erro desconhecido'),
            buttons: ['OK']
          });
          alert.present();
        });
      });
  }

  register(){
    this.navCtrl.push(RegisterPage);
  }

}
