# DECISIONS

## 2026-03 Core decisions

### 1. One main layout
- Decision: use a single shared site shell in `Views/master.cshtml`
- Why:
  - avoid duplicate layout drift
  - keep MVC and Umbraco pages visually identical
  - prevent recurring billing/account layout divergence

### 2. Home node is the primary runtime config source
- Decision: SMTP and Stripe runtime settings are read from the Home node first
- Fallback: `appsettings*.json`
- Why:
  - non-dev admins can change settings without code deploy
  - still safe to boot locally if content values are empty

### 3. My Profile access is role-based
- Decision: My Profile remains protected by Umbraco Public Access
- Required group: `Standard`
- Why:
  - works with Umbraco member protection model already configured in content
  - avoids custom authorization duplication

### 4. Registration and login must enforce `Standard`
- Decision:
  - registration assigns `Standard`
  - login also auto-fixes old members missing `Standard`
- Why:
  - otherwise profile page returns page-not-found due to Public Access mismatch

### 5. Paid content is driven only by actual Umbraco prices
- Decision: only content with a real price value is treated as paid
- Why:
  - default type pricing created incorrect purchase CTAs on all items
  - editors must control paywall behavior through content data

### 6. Stripe webhook scope is intentionally narrow
- Endpoint: `/billing/listen-to-stripe`
- Decision:
  - process only `payment_intent.succeeded`
  - ignore every other event silently
  - do not log ignored events
- Why:
  - reduce noise
  - match the business event used to grant access

### 7. Stripe webhook signature validation is intentionally disabled
- Decision: no webhook secret validation
- Why:
  - explicit project requirement
- Risk:
  - lower security than standard Stripe webhook verification
- Implication:
  - treat endpoint as trusted-environment infrastructure and review before public production rollout

### 8. Cart is cookie-based
- Decision: cart state lives in browser cookie
- Why:
  - simple anonymous-to-member flow
  - no extra storage required
- Implication:
  - webhook cannot clear cart cookie directly
  - purchased pending items are removed on success page request

### 9. Member purchase and activity history is stored on the member record
- Decision:
  - machine-readable history in JSON properties
  - human-readable summaries in separate text properties
- Why:
  - front-end rendering
  - readable backoffice visibility
  - no separate commerce database model required

### 10. Premium LTU visual system
- Decision: keep modern premium LTU style on membership/commerce flow pages.
- Scope now: membership, cart, checkout, success/cancel, auth forms.
- Full-site rollout is deferred to a separate branch/task.

### 11. Build and test must be sequential
- Decision: do not run build and tests in parallel
- Why:
  - Umbraco tooling can lock schema/config output files
  - sequential runs are stable

### 12. Subscription entitlements are duration-based plans
- Decision:
  - subscription checkout is implemented as duration plans (3m / 12m)
  - active plan bypasses per-item paid-content checks
  - plan activation is handled on `payment_intent.succeeded` webhook
- Why:
  - matches donor business behavior
  - simpler than recurring Stripe subscription lifecycle
  - integrates cleanly with existing one-time purchase flow

### 13. New pages must be Umbraco nodes via uSync
- Decision:
  - new public pages should be added as content nodes in `uSync/v17/Content`
  - after adding config, run uSync import locally
- Why:
  - keeps content tree reproducible
  - avoids route drift between environments

### 14. Listing access badges distinguish purchase vs subscription
- Decision:
  - paid listings show `Purchased` only for direct purchases
  - if member has active subscription but no direct purchase, show `Subscribed`
  - free-item primary CTA uses green corporate style (`ltu-flow-btn--accent`)
- Why:
  - avoid misleading entitlement messaging
  - make subscription-based access explicit
  - keep CTA look consistent with LTU commerce style

### 15. Search results are categorized on the search page
- Decision:
  - search results are grouped by category (`Webinars`, `Protocols`, `Videos`, `Research`, `Pages`)
  - ignored doctypes remain excluded from search results
- Why:
  - improves scanability for mixed-content queries
  - aligns search UX with LTU content taxonomy

### 16. Dev-only short subscription plan for expiry testing
- Decision:
  - optional 10-minute plan (`med-10m-test`) is available only when enabled by config
  - webhook/payment metadata supports minute-based subscription durations
  - profile/subscription UI renders human-readable remaining time for short plans
- Why:
  - enables fast local/staging validation of subscription expiration behavior
  - avoids waiting months for lifecycle tests

### 17. Mobile header UX fixes are handled with targeted legacy-safe overrides
- Decision:
  - keep legacy header structure and add member-specific classes for logged-in account rendering
  - enable submenu toggle in burger nav from parent labels (`#` links) in addition to arrow control
  - apply CSS truncation/spacing overrides for long logged-in usernames/emails on small screens
- Why:
  - fixes real mobile usability issues without a full header rewrite
  - avoids regressions in legacy desktop header layout

### 18. Subscription history in My Profile is user-expanded
- Decision:
  - the `Subscription History` panel content is hidden by default
  - history records are revealed only after explicit click on a toggle button
  - existing `Show more` behavior inside history remains unchanged
- Why:
  - prevents large subscription history from pushing key dashboard blocks too far down
  - keeps profile scanability high on both desktop and mobile

### 19. Search and listing UX align with membership visual system
- Decision:
  - search page is rendered as card panels grouped by product type
  - search result titles/excerpts highlight matched query terms
  - listing CTAs use differentiated action roles (`buy`, `cart`, `subscribe`) instead of near-identical color usage
- Why:
  - improves discoverability and visual hierarchy in high-density catalog pages
  - makes commerce actions easier to distinguish at a glance

### 20. Build output warning baseline is enforced at project level
- Decision:
  - update core package baseline to `Umbraco.Cms 17.2.2`
  - set NuGet audit severity threshold to `high` for app and tests
  - suppress legacy nullable/obsolete warning codes currently outside this task scope
  - exclude `artifacts/**` from compile/content item inclusion
- Why:
  - keep CI/local output readable and focused on actionable issues
  - avoid duplicated warnings from publish artifacts and legacy migration debt
