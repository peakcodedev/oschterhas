export interface Environment {
  readonly production: boolean;
  /** Instances live at `<slug>.<baseDomain>`, administration at `<adminSubdomain>.<baseDomain>`. */
  readonly baseDomain: string;
  readonly adminSubdomain: string;
  /** CNAME target for custom domains, shown in the DNS instructions. */
  readonly customDomainTarget: string;
}
