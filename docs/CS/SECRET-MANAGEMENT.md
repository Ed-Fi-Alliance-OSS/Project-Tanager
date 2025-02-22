# Client credential expiration and rotation

Keycloak does not provide a built-in setting for client secret expiration. To
enforce secret rotation and prevent expired credentials from being used for
token issuance, we have a few options:

## **Client Attributes for Metadata**

Use custom client attributes to store a `secret_expiry` timestamp. The vendor
application create response will also include secret_expiry, similar to token expiry.
We can make this configurable in the CMS appsettings(30 days)

  ```json

  {
      "id": 3,
      "key": "361994b9-5a74-4f14-bd9e-6a0bc6309d10",
      "secret": "mPbyZnj2CqH3RvnzOBVYDJZE5INBi88e",
      "secret_expires_on": "2025-04-01T00:00:00Z"
  }
  ```

## Automated Tool

Develop a PowerShell script or console application to assist administrators in
managing client secrets efficiently. This tool will:

* Check `secret_expiry` metadata for all clients in the realm.
* Identify expired secrets by comparing the stored expiry timestamp with the
  current date.

* **Secret Rotation:** Trigger secret rotation for clients with expired
  credentials using the `/v2/applications/{{id}}/reset-credential` CMS endpoint.
  The new credentials will be communicated to the respective clients.

* **Disabling Clients:** As an alternative to automatic secret rotation, the
  script can be configured to disable clients with expired secrets. If a user/
  client application attempts to use an expired secret on the token endpoint,
  they will receive an "Invalid Client" error. Need to prompt the administrator
  to manually call the `/v2/applications/{{id}}/reset-credential` endpoint to
  generate a new secret. This approach ensures that expired secrets cannot be
  used while allowing administrators to have manual control over secret resets.

Between these options, Secret Rotation reduces manual effort and simplifies
secret management for administrators.
