import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { UpdateBanner } from './shared/update/update-banner';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, UpdateBanner],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<router-outlet /><app-update-banner />',
})
export class App {}
