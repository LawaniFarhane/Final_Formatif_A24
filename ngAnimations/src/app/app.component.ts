import { transition, trigger, useAnimation } from '@angular/animations';
import { Component } from '@angular/core';
import { bounce, shakeX, tada } from 'ng-animate';
import { lastValueFrom, timer } from 'rxjs';

@Component({
    selector: 'app-root',
    templateUrl: './app.component.html',
    styleUrls: ['./app.component.css'],
    standalone: true,
    animations: [
      // Animation du carre rouge: le trigger part chaque fois que redShake est incremente.
      trigger('redShake', [transition(':increment', useAnimation(shakeX, { params: { timing: 2 } }))]),
      // Animation du carre vert: elle dure 4 secondes comme demande dans l'enonce.
      trigger('greenBounce', [transition(':increment', useAnimation(bounce, { params: { timing: 4 } }))]),
      // Animation du carre bleu: elle dure 3 secondes et sera lancee pendant la fin du carre vert.
      trigger('blueTada', [transition(':increment', useAnimation(tada, { params: { timing: 3 } }))]),
    ]
})
export class AppComponent {
  title = 'ngAnimations';

  redShake = 0;
  greenBounce = 0;
  blueTada = 0;
  isAnimatingOnce = false;
  isAnimatingLoop = false;
  isRotating = false;

  constructor() {
  }

  async animateOnce() {
    if (this.isAnimatingOnce || this.isAnimatingLoop)
      return;

    // On bloque les clics pendant la sequence pour eviter de partir deux animations en meme temps.
    this.isAnimatingOnce = true;

    await this.playAnimationSequence();

    // La sequence est terminee quand le tada bleu de 3 secondes est fini.
    this.isAnimatingOnce = false;
  }

  async animateLoop() {
    if (this.isAnimatingOnce || this.isAnimatingLoop)
      return;

    // On garde cette valeur a true pour indiquer que la sequence doit recommencer sans arret.
    this.isAnimatingLoop = true;

    while (this.isAnimatingLoop) {
      // On reutilise exactement la meme sequence que le bouton "Animer une fois".
      await this.playAnimationSequence();
    }
  }

  async rotateOnce() {
    if (this.isRotating)
      return;

    // La classe CSS rotate-left est ajoutee seulement pendant les 2 secondes de rotation.
    this.isRotating = true;
    await this.waitFor(2);

    // On retire la classe quand l'animation est finie pour permettre un prochain clic.
    this.isRotating = false;
  }

  private async playAnimationSequence() {
    // Le carre rouge commence immediatement et joue son shake pendant 2 secondes.
    this.redShake++;
    await this.waitFor(2);

    // Quand le rouge est termine, le carre vert commence son bounce de 4 secondes.
    this.greenBounce++;
    await this.waitFor(3);

    // Le bleu commence 1 seconde avant la fin du vert, donc apres 3 des 4 secondes du bounce.
    this.blueTada++;
    await this.waitFor(3);
  }

  private async waitFor(delayInSeconds: number) {
    // timer utilise des millisecondes, donc on convertit les secondes de l'enonce en millisecondes.
    await lastValueFrom(timer(delayInSeconds * 1000));
  }
}
