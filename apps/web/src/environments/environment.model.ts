export interface Environment {
  readonly production: boolean;
  /** Instances live at `<slug>.<baseDomain>`, administration at `<adminSubdomain>.<baseDomain>`. */
  readonly baseDomain: string;
  readonly adminSubdomain: string;
}
