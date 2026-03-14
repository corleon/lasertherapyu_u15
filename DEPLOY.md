# DEPLOY

## Prerequisites
- .NET 10 runtime installed on the target server
- Correct database connection and Umbraco environment config
- Writable app folders for Umbraco runtime files
- Stripe and SMTP settings configured either:
  - on the Home node, or
  - in `appsettings` as fallback

## Publish output
- Folder: `artifacts/publish/LTU_U15`
- Zip: `artifacts/publish/LTU_U15.zip`

## Recommended deployment steps
1. Stop the site / app pool.
2. Back up the site files and database.
3. Deploy published files from `artifacts/publish/LTU_U15`.
4. Ensure config points to the correct database.
5. Start the site.
6. Log into Umbraco backoffice.
7. Run `uSync import` if the target environment does not already have the latest schema/content changes.
8. Republish affected root content if needed.

## Critical content/config items after deploy
- Home node runtime settings:
  - admin notification email
  - Stripe publishable key
  - Stripe secret key
  - Stripe currency
  - SMTP host
  - SMTP port
  - SMTP username
  - SMTP password
  - SMTP from email
  - SMTP SSL flag
- Appsettings/runtime flags:
  - keep `Stripe:EnableTenMinuteTestSubscription=false` in production
- Member type fields from uSync:
  - purchase history fields
  - recent view fields
- Public Access:
  - `My Profile` must still require member group `Standard`

## Production smoke check
1. Home page renders with modern shell.
2. Login and registration pages render.
3. New registration creates member and assigns `Standard`.
4. `/members/my-profile/` opens after login.
5. Paid item redirects to checkout if not purchased.
6. Stripe checkout opens.
7. Successful payment grants access.
8. Cart clears purchased items after success.
9. Registration and purchase emails send.
10. Backoffice member shows purchase history summary.
11. `/search?q=laser` renders categorized result sections.
12. Paid listing badges show `Purchased` only for direct purchases and `Subscribed` for subscription-based access.
13. On mobile viewport, burger menu opens and submenu items expand correctly.
14. On mobile viewport while logged in, header account label is readable (no icon/text overlap).
15. `/members/my-profile/` shows subscription history collapsed by default and expands on button click.
16. `/search?q=laser` renders panelized product-type sections and highlights matched keyword terms.
17. Paid listing cards render distinct CTA styles for `Buy Now`, `Add to Cart`, and `Subscribe`.

## Local publish command
```powershell
dotnet publish .\LTU_U15.csproj -c Release -o .\artifacts\publish\LTU_U15 /p:UseSharedCompilation=false
Compress-Archive -Path .\artifacts\publish\LTU_U15\* -DestinationPath .\artifacts\publish\LTU_U15.zip -Force
```

## Common deployment issues

### Build blocked by locked exe
```powershell
Get-Process LTU_U15 -ErrorAction SilentlyContinue | Stop-Process -Force
```

### Missing content type/member type changes
- Run `uSync import`

### Static files look old after deploy
- Hard refresh browser cache
- verify `ltu-site-theme.css` and `ltu-commerce-ui.css` are present on server
