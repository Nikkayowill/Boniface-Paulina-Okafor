# Frontend-to-Backend Integration Contract

This guide is for the frontend workspace of the Okafor Hospital PWA. The goal is simple: keep the user interface fast, reliable, and safe on slow rural networks while letting the ASP.NET backend handle security, payments, patient data, and database writes.

You can build beautiful HTML, CSS, and vanilla JavaScript without writing C#. The main rule is: the frontend talks to our backend API, and the backend talks to databases, Paystack, Flutterwave, email, SMS, and other private services.

## 1. Monorepo Structure And Architecture

Target structure:

```text
Okafor-.NET/
  Okafor-.NET.csproj
  Program.cs
  Controllers/
  Services/
  Models/
  Data/

  Frontend/
    index.html
    manifest.json
    sw.js

    css/
      app.css

    js/
      app.js
      api.js
      payments.js

    images/
      icons/

    mock-data/
      mock-patients.json
```

Current repo note: this project currently serves public assets from `wwwroot/`. Existing PWA files are currently named `wwwroot/site.webmanifest` and `wwwroot/service-worker.js`. The target frontend contract uses `/Frontend/manifest.json` and `/Frontend/sw.js`; if the backend keeps using `wwwroot`, treat `wwwroot` as the deployed static web root and keep the same rules.

Recommended frontend file responsibilities:

```text
Frontend/index.html          Main app shell loaded by the browser.
Frontend/css/app.css         Shared lightweight styles.
Frontend/js/app.js           Page startup and event binding.
Frontend/js/api.js           Shared fetch helpers, timeout wrapper, API base URL.
Frontend/js/payments.js      Payment button and payment initialization flow.
Frontend/manifest.json       PWA install metadata.
Frontend/sw.js               Service worker for safe public/offline assets.
Frontend/mock-data/          Local static JSON files for UI testing.
```

Important rule: do not place C# files, API secrets, database details, Paystack secret keys, Flutterwave secret keys, or admin-only data inside `Frontend/`.

### Static File Serving

The ASP.NET backend serves the frontend as static files. Conceptually, the backend setup looks like this:

```csharp
app.UseStaticFiles();
```

If we use a physical `/Frontend` folder instead of `wwwroot`, the backend will explicitly serve that folder:

```csharp
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(builder.Environment.ContentRootPath, "Frontend")),
    RequestPath = ""
});
```

As a frontend designer, you do not need to edit this backend setup. Just keep assets in the agreed frontend folder and reference them with root-relative URLs:

```html
<link rel="manifest" href="/manifest.json">
<link rel="stylesheet" href="/css/app.css">
<script src="/js/app.js" defer></script>
```

### Frontend Route Fallbacks

If the frontend has client-side routes like these:

```text
/patients
/patients/123
/payments
/appointments/new
```

the browser may request those URLs directly when a user refreshes the page or opens a shared link. Without a fallback, ASP.NET may return `404 Not Found` because there is no physical file called `/patients/123`.

The backend prevents this with `MapFallbackToFile`:

```csharp
app.MapFallbackToFile("index.html");
```

That means:

```text
User opens /patients/123
ASP.NET cannot find a matching controller route or static file
ASP.NET sends Frontend/index.html
Frontend JavaScript reads the URL and renders the correct screen
```

Frontend responsibility: use normal links and browser URLs, but keep route names clean and predictable. Avoid route names that collide with backend API routes.

Reserved backend route prefixes:

```text
/api/*
/Admin/*
/Portal/*
/Identity/*
/BillPayments/*
/Donation/*
/uploads/*
/hubs/*
```

Do not create frontend routes under those prefixes unless we agree first.

## 2. Secure Payment Workflow: Paystack / Flutterwave

Never initialize Paystack or Flutterwave payments directly from frontend JavaScript.

Reason: payment providers require secret keys for secure initialization and verification. If a secret key is placed in JavaScript, every browser user can see it. That can lead to fraud, fake transactions, altered amounts, and leaked hospital payment credentials.

Correct workflow:

```text
User taps payment button
Frontend disables the button and shows loading
Frontend sends payment request to /api/payments/initialize
ASP.NET validates amount, invoice, patient, and idempotency
ASP.NET calls Paystack or Flutterwave using private secret keys
ASP.NET returns a hosted checkout URL
Frontend redirects the browser to that secure checkout URL
```

Expected backend endpoint:

```http
POST /api/payments/initialize
Content-Type: application/json
Idempotency-Key: <stable unique key for this attempt>
```

Expected request body:

```json
{
  "invoiceNumber": "INV-2026-00042",
  "patientName": "Ada Okafor",
  "patientEmail": "ada@example.com",
  "patientPhone": "+2348012345678",
  "amount": 15000,
  "currency": "NGN",
  "purpose": "Hospital bill payment"
}
```

Expected successful response:

```json
{
  "checkoutUrl": "https://checkout.paystack.com/secure-generated-url",
  "reference": "BILL-123-20260524143000",
  "provider": "Paystack"
}
```

### Vanilla JavaScript Payment Example

HTML:

```html
<form id="payment-form">
  <input id="invoice-number" name="invoiceNumber" required>
  <input id="patient-name" name="patientName" required>
  <input id="patient-email" name="patientEmail" type="email" required>
  <input id="patient-phone" name="patientPhone" required>
  <input id="amount" name="amount" type="number" min="1" required>

  <button id="pay-button" type="submit">
    Pay now
  </button>

  <p id="payment-message" role="status" aria-live="polite"></p>
</form>
```

JavaScript:

```js
const paymentForm = document.querySelector("#payment-form");
const payButton = document.querySelector("#pay-button");
const paymentMessage = document.querySelector("#payment-message");

paymentForm.addEventListener("submit", async function (event) {
    event.preventDefault();

    if (payButton.disabled) {
        return;
    }

    payButton.disabled = true;
    payButton.textContent = "Preparing secure checkout...";
    paymentMessage.textContent = "Please wait. Do not tap twice.";

    const idempotencyKey = getOrCreatePaymentAttemptKey();

    const payload = {
        invoiceNumber: document.querySelector("#invoice-number").value.trim(),
        patientName: document.querySelector("#patient-name").value.trim(),
        patientEmail: document.querySelector("#patient-email").value.trim(),
        patientPhone: document.querySelector("#patient-phone").value.trim(),
        amount: Number(document.querySelector("#amount").value),
        currency: "NGN",
        purpose: "Hospital bill payment"
    };

    try {
        const response = await fetchWithTimeout("/api/payments/initialize", {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "Idempotency-Key": idempotencyKey
            },
            body: JSON.stringify(payload)
        }, 15000);

        if (!response.ok) {
            throw new Error("Unable to start payment. Please check the details and try again.");
        }

        const result = await response.json();

        if (!result.checkoutUrl) {
            throw new Error("Payment checkout link was not returned.");
        }

        paymentMessage.textContent = "Redirecting to secure checkout...";
        window.location.assign(result.checkoutUrl);
    } catch (error) {
        payButton.disabled = false;
        payButton.textContent = "Pay now";
        paymentMessage.textContent = error.message || "Connection slow, please try again.";
    }
});

function getOrCreatePaymentAttemptKey() {
    const storageKey = "okafor-payment-attempt-key";
    const existingKey = sessionStorage.getItem(storageKey);

    if (existingKey) {
        return existingKey;
    }

    const newKey = crypto.randomUUID
        ? crypto.randomUUID()
        : `${Date.now()}-${Math.random().toString(16).slice(2)}`;

    sessionStorage.setItem(storageKey, newKey);
    return newKey;
}
```

After a successful redirect to Paystack or Flutterwave, do not remove the idempotency key manually. The browser is leaving the page. The backend will verify the payment using provider webhooks and callback verification.

## 3. Idempotency And Rural UX Patterns: Anti-Glitch Code

Slow networks create dangerous UI problems:

```text
User taps twice because nothing changed
Browser resends the same request
Patient gets created twice
Payment gets initialized twice
Staff lose trust in the app
```

The frontend must always give immediate feedback for any form that creates or pays for something.

### Disable Submit Immediately

Use this pattern for patient creation, appointment requests, bill payment, donation, document upload, or anything that writes data.

```html
<form id="patient-form">
  <input id="full-name" name="fullName" required>
  <input id="phone-number" name="phoneNumber" required>

  <button id="save-patient-button" type="submit">
    Save patient
  </button>

  <p id="patient-message" role="status" aria-live="polite"></p>
</form>
```

```js
const patientForm = document.querySelector("#patient-form");
const savePatientButton = document.querySelector("#save-patient-button");
const patientMessage = document.querySelector("#patient-message");

patientForm.addEventListener("submit", async function (event) {
    event.preventDefault();

    if (savePatientButton.disabled) {
        return;
    }

    savePatientButton.disabled = true;
    savePatientButton.textContent = "Saving...";
    patientMessage.textContent = "Saving patient. Please wait.";

    const payload = {
        fullName: document.querySelector("#full-name").value.trim(),
        phoneNumber: document.querySelector("#phone-number").value.trim()
    };

    try {
        const response = await fetchWithTimeout("/api/patients", {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "Idempotency-Key": getOneTimeActionKey("create-patient")
            },
            body: JSON.stringify(payload)
        }, 15000);

        if (!response.ok) {
            throw new Error("Patient could not be saved. Please try again.");
        }

        patientMessage.textContent = "Patient saved.";
        patientForm.reset();
    } catch (error) {
        savePatientButton.disabled = false;
        savePatientButton.textContent = "Save patient";
        patientMessage.textContent = error.message || "Connection slow, please try again.";
    }
});

function getOneTimeActionKey(actionName) {
    return `${actionName}-${Date.now()}-${Math.random().toString(16).slice(2)}`;
}
```

Button rule:

```text
Disable immediately when the request starts.
Keep disabled while the request is in flight.
Re-enable only if the request fails.
Do not re-enable after success unless the user must submit a new, different record.
```

### Standard Fetch Timeout Wrapper

Every important API call should use this wrapper. It aborts the request if the hospital connection hangs for more than 15 seconds.

```js
async function fetchWithTimeout(url, options = {}, timeoutMs = 15000) {
    const controller = new AbortController();
    const timeoutId = window.setTimeout(function () {
        controller.abort();
    }, timeoutMs);

    try {
        const response = await fetch(url, {
            ...options,
            signal: controller.signal
        });

        return response;
    } catch (error) {
        if (error.name === "AbortError") {
            throw new Error("Connection slow, please try again.");
        }

        throw error;
    } finally {
        window.clearTimeout(timeoutId);
    }
}
```

Example use:

```js
try {
    const response = await fetchWithTimeout("/api/patients", {
        method: "GET",
        headers: {
            "Accept": "application/json"
        }
    }, 15000);

    const patients = await response.json();
    renderPatients(patients);
} catch (error) {
    alert(error.message || "Connection slow, please try again.");
}
```

### Rural UX Rules

Follow these rules on every screen:

```text
Show loading text immediately after a tap.
Use one network request per user action.
Never let users tap payment or save buttons repeatedly.
Keep messages short and calm.
Do not clear entered form data after a failed request.
Do not cache private patient, billing, or medical data in the service worker.
Use mock data for layout work, not copied real patient data.
```

## 4. Mocking Data For Local Design Testing

You can test layouts without the backend running by using static mock JSON.

Place mock files here:

```text
Frontend/
  mock-data/
    mock-patients.json
```

Example `mock-patients.json`:

```json
[
  {
    "id": 1,
    "fullName": "Ada Okafor",
    "age": 34,
    "phoneNumber": "+2348012345678",
    "lastVisit": "2026-05-12",
    "status": "Waiting"
  },
  {
    "id": 2,
    "fullName": "Chinedu Nwosu",
    "age": 48,
    "phoneNumber": "+2348098765432",
    "lastVisit": "2026-05-18",
    "status": "Checked in"
  }
]
```

Quick API switch:

```js
const USE_MOCK_DATA = true;

const API_URL = USE_MOCK_DATA
    ? "/mock-data/mock-patients.json"
    : "/api/patients";

async function loadPatients() {
    const response = await fetchWithTimeout(API_URL, {
        method: "GET",
        headers: {
            "Accept": "application/json"
        }
    }, 15000);

    if (!response.ok) {
        throw new Error("Unable to load patients.");
    }

    const patients = await response.json();
    renderPatients(patients);
}

function renderPatients(patients) {
    const list = document.querySelector("#patient-list");
    list.innerHTML = "";

    patients.forEach(function (patient) {
        const item = document.createElement("li");
        item.textContent = `${patient.fullName} - ${patient.status}`;
        list.appendChild(item);
    });
}
```

Recommended local workflow:

```text
Use USE_MOCK_DATA = true while designing screens.
Switch to USE_MOCK_DATA = false when testing real backend integration.
Keep mock data realistic but fake.
Never paste real patient records into mock JSON.
```

## Frontend And Backend Agreement

The frontend owns:

```text
HTML structure
CSS layout and responsive design
Vanilla JavaScript interactions
Loading states
Offline public pages
Mock data for design testing
Accessible messages and button states
```

The backend owns:

```text
Authentication and authorization
Patient data validation
Database writes
Payment initialization
Payment verification
Paystack and Flutterwave secret keys
Idempotency enforcement
Audit logs
Webhooks
Private medical and billing data
```

Shared rules:

```text
Frontend sends JSON to /api/* endpoints.
Backend returns JSON from /api/* endpoints.
Frontend never stores secrets.
Frontend never assumes a payment succeeded just because the user returned from checkout.
Backend is the source of truth for payment status, patient records, and appointments.
All slow-network flows must handle timeout, retry, and duplicate-tap protection.
```

## Final Checklist Before A Screen Is Ready

Use this checklist before handing a frontend screen to backend integration:

```text
The page works on a narrow mobile screen.
The page loads without large unnecessary assets.
Buttons show loading states immediately.
Submit buttons cannot be double-tapped.
fetchWithTimeout is used for API calls.
Errors are shown in plain language.
Form data is not erased after network failure.
Mock data can be switched to live API data in one place.
No secret keys exist in HTML, CSS, JavaScript, JSON, or comments.
No private patient or billing data is cached by the service worker.
```

Keep the frontend calm and predictable. On a weak hospital connection, the best interface is the one that tells users what is happening, prevents accidental duplicates, and never asks them to guess whether their tap worked.
