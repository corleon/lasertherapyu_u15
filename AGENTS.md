# LTU_U15 Project Instructions

Read these files before making non-trivial changes:
- `PROJECT-CONTEXT.md`
- `DECISIONS.md`
- `DEPLOY.md`
- `TESTING.md`

## Project
- Project: Laser Therapy University site on Umbraco 17
- Stack: ASP.NET Core, Umbraco CMS, uSync, Stripe, custom member flow, custom cart
- Target framework: `net10.0`

## Default working rules
- Do not use git unless the user explicitly asks.
- Prefer changing code and config in the repo over manual database edits.
- Prefer uSync-backed changes for content types, templates, member types, and content structure when practical.
- Preserve the current baseline UI (modernized membership/commerce flow + legacy rest-of-site) unless user asks otherwise.
- Use the shared site shell in `Views/master.cshtml` for both Umbraco pages and MVC pages unless there is a concrete technical blocker.

## Important project conventions
- Membership public pages live under `/members/...`
- MVC account endpoints live under `/account/...`
- Billing endpoints live under `/billing/...`
- Cart page lives at `/cart`
- Subscriptions page lives at `/subscriptions` and is expected to be an Umbraco node (doctype `subscriptionsPage`, template `subscriptionsPage`)
- Member dashboard canonical URL: `/members/my-profile/`
- Members root should redirect to `/members/my-profile/`
- My Profile access depends on Umbraco Public Access and member group `Standard`
- New members must be assigned to `Standard`
- For newly added public pages: create uSync content config and run uSync import locally.

## Settings
- Runtime SMTP and Stripe settings are read from the Home node first.
- `appsettings*.json` is only a fallback.
- Home node key is documented in `PROJECT-CONTEXT.md`.

## UI / frontend
- Global shell and page theme:
  - `Views/master.cshtml`
  - `wwwroot/css/ltu-site-theme.css`
- Membership / commerce UI layer:
  - `wwwroot/css/ltu-commerce-ui.css`
- Keep the site in the current LTU style: premium, modern, restrained, green/blue/charcoal palette, serif headlines.

## Build and test
- Run build and tests sequentially, not in parallel.
- Use:
  - `dotnet build .\LTU_U15.csproj -c Debug /p:UseSharedCompilation=false`
  - `dotnet test .\LTU_U15.Tests\LTU_U15.Tests.csproj -c Debug /p:UseSharedCompilation=false`
- If build fails with file locks, stop `LTU_U15.exe` first.

## Logs
- Umbraco logs: `umbraco/Logs/`

## Publish output
- Last publish output path:
  - `artifacts/publish/LTU_U15`
  - `artifacts/publish/LTU_U15.zip`
