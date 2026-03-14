# PROJECT CONTEXT

## Overview
- Project: Laser Therapy University (`LTU_U15`)
- Stack: Umbraco 17.2.1 + ASP.NET Core `net10.0` + uSync 17 + Stripe
- Core commerce model: one-time content purchases + time-based subscriptions

## Current UI baseline (important)
- Active baseline branch is **not** the full-site redesign.
- Modernized in current working branch:
  - membership flow pages
  - cart
  - checkout
  - checkout success/cancel
  - login/register/forgot forms
- The rest of the site still uses legacy look and should be modernized later in a separate effort.

## Header behavior
- Mobile burger navigation supports expanding submenus by tapping either:
  - the expand arrow
  - the parent item label (for `#`/toggle-only parent links)
- Logged-in account label in the header now uses member-specific classes and truncates long usernames/emails safely on small screens.

## Routing and rendering
- Shared shell: `Views/master.cshtml`
- Generic content template: `Views/content.cshtml`
- Membership pages are resolved by URL segment inside `content.cshtml` and rendered via partials.
- `Members` root (`/members/`) should redirect to `/members/my-profile/`.

## Settings source
- Runtime settings are read from Home node first, then `appsettings*.json` fallback.
- Home node key: `dcf18a51-6919-4cf8-89d1-36b94ce4d963`
- Home node stores SMTP + Stripe + admin notification email values.

## Membership and access
- Member type: `Member`
- Required member group for protected profile: `Standard`
- Registration assigns `Standard`
- Login auto-fixes missing `Standard` for legacy members
- Logged-in header shows username instead of `Log In/Register`
- My Profile subscription history panel is collapsed by default and expands on explicit user action.

## Commerce
- Paid content is detected strictly by Umbraco item price values.
- Cart is cookie-based.
- Stripe content checkout endpoints:
  - `POST /billing/create-session`
  - `POST /billing/listen-to-stripe`
- Webhook processes only `payment_intent.succeeded`.
- Stripe description format: `Payment for: {productName}`
- After successful purchase, member history fields are updated and cart is cleaned.
- Listing entitlement labels:
  - show `Purchased` only for direct per-item purchases
  - show `Subscribed` when access comes from active subscription without direct purchase

## Subscriptions
- Public page URL: `/subscriptions` (served by Umbraco node, doctype/template `subscriptionsPage`)
- Success page URL: `/subscriptions/success` (doctype/template `subscriptionsSuccessPage`)
- Checkout endpoint: `POST /subscriptions/create-session`
- Plans: 3 months / 12 months
- Optional dev-only test plan:
  - code: `med-10m-test`
  - duration: 10 minutes (configurable)
  - enabled via `Stripe:EnableTenMinuteTestSubscription`
- Active subscription grants access to any paid content
- Subscription activation runs from Stripe `payment_intent.succeeded`

## Search
- Search page template: `Views/search.cshtml`
- Search results are grouped by category:
  - `Webinars`
  - `Protocols`
  - `Videos`
  - `Research`
  - `Pages`
- Search UI uses membership-style card panels by product type and highlights matched keywords in result titles/excerpts.
- Ignored doctypes: `category`, `categoryList`, `error`, `search`, `xMLSitemap`

## Listing CTAs
- Paid listing action buttons use role-based visual hierarchy:
  - `Buy Now` (`ltu-flow-btn--buy`)
  - `Add to Cart` (`ltu-flow-btn--cart`)
  - `Subscribe` (`ltu-flow-btn--subscribe`)

## Email notifications
- User + admin emails on registration
- User + admin emails on successful purchase
- SMTP uses `System.Net.Mail.SmtpClient`

## uSync rules
- New pages should be created as Umbraco nodes via `uSync/v17/Content/*.config`.
- After adding/updating uSync configs, run import locally so node appears immediately.

## Key files
- Membership:
  - `Controllers/AccountController.cs`
  - `Views/Partials/membership/loginForm.cshtml`
  - `Views/Partials/membership/registerForm.cshtml`
  - `Views/Partials/membership/forgotPasswordForm.cshtml`
  - `Views/Partials/membership/forgotUsernameForm.cshtml`
  - `Views/Partials/membership/myProfile.cshtml`
- Commerce:
  - `Controllers/BillingController.cs`
  - `Controllers/CartController.cs`
  - `Controllers/SubscriptionsController.cs`
  - `Services/Commerce/ContentPurchaseService.cs`
  - `Services/Commerce/StripePaymentGateway.cs`
- Settings/notifications:
  - `Services/Site/SiteSettingsService.cs`
  - `Services/Membership/MembershipEmailService.cs`
  - `Services/Membership/MembershipNotificationService.cs`
- Search:
  - `Views/search.cshtml`

## Build/test constraints
- Run build and tests sequentially.
- If build fails on locked `LTU_U15.exe`, stop the running process first.
- Current baseline is configured for warning-clean build/test output (legacy nullable/obsolete noise suppressed at project level).
