# TESTING

## Automated

Run sequentially:

```powershell
dotnet build .\LTU_U15.csproj -c Debug /p:UseSharedCompilation=false
dotnet test .\LTU_U15.Tests\LTU_U15.Tests.csproj -c Debug /p:UseSharedCompilation=false
```

If the app is already running and build is locked:

```powershell
Get-Process LTU_U15 -ErrorAction SilentlyContinue | Stop-Process -Force
```

## Local run
```powershell
dotnet run --project .\LTU_U15.csproj
```

## Manual regression checklist

### Membership
1. Open `/members/log-in/`
2. Open `/members/registration/`
3. Register a new member
4. Confirm redirect to `/members/my-profile/`
5. Confirm the member can access My Profile without manual role fixes
6. Log out and log in again
7. Confirm header switches to username when logged in

### Forgot flows
1. Submit forgot username
2. Submit forgot password
3. Confirm banners look correct
4. Confirm no hard crash if SMTP fails

### Paid content
1. Open a free item
2. Confirm it opens directly
3. Open a paid item when not logged in
4. Confirm redirect to login
5. After login, confirm return to checkout/content flow
6. Complete purchase
7. Confirm paid item opens afterward
8. Activate a subscription and confirm any paid item opens without per-item purchase

### Cart
1. Add multiple paid items from listings
2. Open `/cart`
3. Remove one item
4. Clear cart
5. Add multiple items again
6. Complete Stripe checkout
7. Confirm purchased items are removed from cart after success
8. With active subscription, confirm cart auto-clears paid items

### Subscriptions
1. Open `/subscriptions`
2. Anonymous user should see login/register CTA
3. Logged-in user should see plan CTA
4. Complete subscription checkout
5. Confirm redirect to `/subscriptions/success`
6. Confirm profile shows active subscription and expiry

### Profile
1. Open `/members/my-profile/`
2. Confirm purchased products render
3. Confirm recently viewed renders
4. Confirm each section is capped and has `Show more`
5. Confirm `Open` buttons work

### Emails
1. Register a new user
2. Confirm user registration email
3. Confirm admin registration email
4. Complete a purchase
5. Confirm user purchase email
6. Confirm admin purchase email

## Stripe local webhook testing

Run site locally, then forward Stripe events:

```powershell
stripe listen --events payment_intent.succeeded --forward-to https://localhost:44362/billing/listen-to-stripe --skip-verify
```

Important:
- webhook only processes `payment_intent.succeeded`
- other events are intentionally ignored
- webhook signature validation is intentionally disabled in this project

## uSync import (local)

Run app once with startup import enabled:

```powershell
dotnet run --project .\LTU_U15.csproj -- --uSync:Settings:ImportAtStartup=All
```

After startup completes, stop the app and run normally.

## Logs
- Umbraco logs: `umbraco/Logs/`

Useful log cases:
- Stripe webhook processing failures
- purchase recorded successfully
- registration notification email sending failed
- purchase notification email sending failed
- forgot password / forgot username email sending failed

## Common failures

### My Profile shows page not found
- member is missing group `Standard`
- login flow should auto-fix this now

### Build/test conflict
- run sequentially only

### Checkout succeeds but access is not granted
- inspect Stripe webhook delivery
- inspect `payment_intent.succeeded`
- inspect member purchase fields in backoffice

### Cart still contains purchased items
- open success/profile page after payment
- cart cleanup happens on the post-payment browser request
