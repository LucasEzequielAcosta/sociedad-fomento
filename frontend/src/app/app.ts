import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

/** Root component for the application shell. */
@Component({
  imports: [RouterOutlet],
  selector: 'app-root',
  styleUrl: './app.scss',
  templateUrl: './app.html',
})
export class App {}
