# IdentityDemo: Entra vs Auth0 token claims

This API accepts JWT access tokens from two identity providers and routes each token by its issuer.

| Claim | Entra ID | Auth0 |
|---|---|---|
| Issuer (`iss`) | https://sts.windows.net/<tenant-id>/ | https://<tenant>.us.auth0.com/ |
| Audience (`aud`) | api://<api-client-id> | https://identitydemo-api |
| Subject (`sub`) | Pairwise: unique per user per app | `<client-id>@clients` (the calling app, not a person) |
| Stable user ID | `oid` (same across apps in the tenant) | None, because this token has no user |
| Scopes | `scp` = access_as_user | None in this token (see note) |
| Roles | None in this token | None in this token |
| Grant type (`gty`) | Not present (delegated user token) | `client-credentials` (machine-to-machine) |
| Authorized party | `appid` = calling app ID | `azp` = calling app's client ID |
| User details | `name`, `email`, `given_name`, `family_name` | None |
| Token version | `ver` = 1.0 | n/a |
| Lifetime | About 1 hour | About 24 hours |

## What I had to handle
- Routing: one policy scheme reads the unverified issuer and forwards to the matching JWT bearer handler.
- Validation: each provider has its own authority and audience, validated separately.
- Mapping: I would convert each provider's claims into one internal identity (a user ID and a list of permissions) right after the token is validated. I would prefix the ID with the provider name, such as `entra:<oid>` and `auth0:<sub>`, so IDs from different providers can never collide.
- Different token types: the Entra token represents a signed-in person, while the Auth0 token represents an application. An API serving both needs to know which kind it received before it decides what the caller may do.