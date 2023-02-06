# config-service

An API for changing parameters for various Rumble services almost instantaneously.

# Introduction

The original Dynamic Config was a staple of Rumble's servers.  It was a very simple service: there was a "scope", such as for a particular game key, and a dictionary of string values and string keys.  At its core, this was all the service was.  As a downside, however, there were several scopes that games relied on, and they weren't all found in the most intuitive places.

This new service seeks to add functionality and provide a more polished experience.  In addition, there were features that never worked properly, namely the ability to subscribe to the service and be notified instantly if anything changed.  As this project grows, that functionality will be built in.

There is some new terminology so as to differentiate this project from its predecessor, as covered in the Glossary.

Platform-common includes a singleton service that grabs all of the values automatically.  It is not necessary to hit any of the endpoints here unless you're working specifically on platform-common or have some other special need to do so.

As many of the services will require the config service to retrieve their admin tokens, the initial GET request must be secured by Rumble secrets.

# Glossary

| Term               | Definition                                                                                                                                                                                                                                          |
|:-------------------|:----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Registered Service | Every platform project that uses the config-service is considered a Registered Service when it successfully connects.  This happens in Startup as part of platform-common.  A service must be registered to be actively notified of config changes. |
| Section            | The replacement for "scope".  A Section contains all of the information for a particular project, including every registered service, an admin token, and all of the Settings.                                                                      |
| Setting            | A value and a comment to document the value.                                                                                                                                                                                                        |

# environment.json

To run locally, all platform-services require an `environment.json` file in the top directory of the solution.  This includes the `PLATFORM_COMMON` value, which is shared across all services with later platform-common packages.

```
{
  "MONGODB_NAME": "config-service-107",
  "RUMBLE_COMPONENT": "config-service",
  "RUMBLE_REGISTRATION_NAME": "Dynamic Config V2",
  "RUMBLE_DEPLOYMENT": "007",
  "PLATFORM_COMMON": {
    "MONGODB_URI": {
      "*": "mongodb://localhost:27017/leaderboard-service-107?retryWrites=true&w=majority&minPoolSize=2"
    },
    "CONFIG_SERVICE_URL": {
      "*": "https://config-service.cdrentertainment.com/"
    },
    "GAME_GUKEY": {
      "*": "{redacted}"
    },
    "GRAPHITE": {
      "*": "graphite.rumblegames.com:2003"
    },
    "LOGGLY_BASE_URL": {
      "*": "https://logs-01.loggly.com/bulk/f91d5019-e31d-4955-812c-31891b64b8d9/tag/{0}/"
    },
    "RUMBLE_KEY": {
      "*": "{redacted}"
    },
    "RUMBLE_TOKEN_VALIDATION": {
      "*": "https://dev.nonprod.tower.cdrentertainment.com/token/validate"
    },
    "SLACK_ENDPOINT_POST_MESSAGE": {
      "*": "https://slack.com/api/chat.postMessage"
    },
    "SLACK_ENDPOINT_UPLOAD": {
      "*": "https://slack.com/api/files.upload"
    },
    "SLACK_ENDPOINT_USER_LIST": {
      "*": "https://slack.com/api/users.list"
    },
    "SLACK_LOG_BOT_TOKEN": {
      "*": "xoxb-4937491542-3072841079041-s1VFRHXYg7BFFGLqtH5ks5pp"
    },
    "SLACK_LOG_CHANNEL": {
      "*": "C031TKSGJ4T"
    },
    "SWARM_MODE": {
      "*": false
    },
    "VERBOSE_LOGGING": {
      "*": false
    }
  }
}
```

# Class Overview

## Controllers
| Name               | Description                              |
|:-------------------|:-----------------------------------------|
| SettingsController | Handles CRUD operations for settings.    |
| TopController      | Handles service registration and health. |

## Exceptions
| Name | Description |
|:-----|:------------|
|      |             |

## Models
| Name              | Description                                                                                                                                                                                                                                                                                                                                             |
|:------------------|:--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| RegisteredService | Registered Services represent individual containers for every Platform project.  So, if Leaderboards has six containers, there will be six registered leaderboards services.  Each registered service has a version, platform-common version, information on when it was last updated / active, project owner, and information on all of its endpoints. |
| Section           | The meat of the service.  A Section contains all information every service will need, but also information that the Portal will use.  Consequently, information is sanitized for standard platform-common usage.                                                                                                                                        |
| SettingsValue     | A very simple model consisting of simply a value object and an internal comment to document it.                                                                                                                                                                                                                                                         |

**Important note**

Because the config service is necessarily used in all other Platform projects, the models' code files for this service are instead found in platform-common (1.1.43+), though they are documented here.

## Services
| Name               | Description                                                                                                                                                     |
|:-------------------|:----------------------------------------------------------------------------------------------------------------------------------------------------------------|
| DeregisterService  | This service runs at a very long interval and removes inactive registered services.  Its only job is to act as a janitor and prevent unnecessary data build-up. |
| SectionService     | Handles CRUD operations for Sections.                                                                                                                           |

## Endpoints

All endpoints live off of the base `{platform url}/config`.

### Top level

Player authorization tokens are required.

| Method | Endpoint    | Description                                                                                                  | Required Fields                                               | Optional Fields |
|-------:|:------------|:-------------------------------------------------------------------------------------------------------------|:--------------------------------------------------------------|:----------------|
|  PATCH | `/register` | Registers a service to the relevant section.                                                                 | `RUMBLE_COMPONENT`<br>`RUMBLE_REGISTRATION_NAME`<br>`service` ||

Registration happens during Startup in platform-common and requires Rumble secrets (game secret + Rumble secret) to be successful.  This is handled automatically by the DC2Service.  Example request:

**PATCH http://localhost:5151/config/register?game={redacted}&secret={redacted}**
```
{
    "RUMBLE_COMPONENT": "dynamic-config-service",       // Used to recognize services by their internal names
    "RUMBLE_REGISTRATION_NAME": "Dynamic Config V2",    // Used by Portal to display a cleaner name to users
    "service": {
        "RUMBLE_DEPLOYMENT": "007",
        "rootIngress": "config",
        "version": "1.0.0.0",
        "commonVersion": "1.1.21",
        "endpoints": [
            "DELETE /config/cachedToken",
            "GET /config/environment",
            "GET /config/health",
            "PATCH /config/register"
        ],
        "controllers": [
            {
                "routes": [
                    "config"
                ],
                "endpoints": [
                    "DELETE /config/cachedToken",
                    "GET /config/environment",
                    "GET /config/health",
                    "PATCH /config/register"
                ],
                "methods": [
                    {
                        "httpMethods": [
                            "DELETE"
                        ],
                        "authorizationType": "Admin Token",
                        "methodName": "DeleteCachedToken",
                        "path": "DELETE /config/cachedToken",
                        "routes": [
                            "/config/cachedToken"
                        ]
                    },
                    {
                        "httpMethods": [
                            "GET"
                        ],
                        "authorizationType": "Rumble Keys",
                        "methodName": "GetEnvironmentVariables",
                        "path": "GET /config/environment",
                        "routes": [
                            "/config/environment"
                        ]
                    },
                    {
                        "httpMethods": [
                            "GET"
                        ],
                        "authorizationType": "None",
                        "methodName": "HealthCheck",
                        "path": "GET /config/health",
                        "routes": [
                            "/config/health"
                        ]
                    },
                    {
                        "httpMethods": [
                            "PATCH"
                        ],
                        "authorizationType": "Rumble Keys",
                        "methodName": "Register",
                        "path": "PATCH /config/register",
                        "routes": [
                            "/config/register"
                        ]
                    }
                ],
                "controllerName": "TopController"
            }
        ],
        "owner": "Will"
    }
}
```

### Settings

Player authorization tokens are required.

| Method | Endpoint          | Description                                                                                                                                   | Required Fields                         | Optional Fields |
|-------:|:------------------|:----------------------------------------------------------------------------------------------------------------------------------------------|:----------------------------------------|:----------------|
|    GET | `/settings`       | Returns the recommended, cleaned set of values to a registered service.                                                                       |                                         | `name`          |
|    GET | `/settings/all`   | Returns all Sections the config service is tracking.  This should only ever be used by Portal for management purposes.                        |                                         |
|   POST | `/settings/new`   | Manually creates a new section.                                                                                                               | `name`<br>`friendlyName`                ||
|  PATCH | `/settings/update | Changes a value, or updates its comment.  After changing, notifications to registered services are sent out so they can fetch the new values. | `name`<br>`key`<br>`value`<br>`comment` ||
| DELETE | `/settings/value  | Deletes a setting permanently.                                                                                                                | `name`<br>`key`                         ||

`GET /settings?game={redacted}&secret={redacted}`

This endpoint returns a sanitized and simplified version of all of the data.  While it is possible to retrieve config data from other services, this practice is _highly_ discouraged.  Registered services should only use their section or the two special sections, global and common, for anything shared between other services.

Response:

```
{
    "global-config": {                                        // Registered RUMBLE_COMPONENT
        "adminToken": "(local token generation unavailable)", // The admin token to use for this particular component
        "foo": "bar"                                          // Any other values specific to the project appear here
    },
    "platform-common": {
        "adminToken": "(local token generation unavailable)"
    },
    "dynamic-config-service": {
        "adminToken": "(local token generation unavailable)"
    }
}
```

<hr />

`GET /settings/all?game={redacted}&secret={redacted}`

Due to sheer length, a sample response cannot be cleanly included here.  However, it is worth noting that this is serialized and deserialized the same way automatically by platform-common.  As long as config-service remains up to date with platform-common, a sample should be unnecessary.  Please refer to the section **Populating the Portal** for usage.

<hr />

`POST /settings/new?game={redacted}&secret={redacted}`

This endpoint can be used to create new Sections in the Portal.  This should not be common practice, as sections are automatically created for every project on startup.  Successful responses return 204 (no content).

<hr />

`PATCH /settings/update`

Requires an admin token.  Successful responses return 204 (no content).

```
{
    "name": "platform-common",
    "key": "pi",
    "value": 3.1415926,
    "comment": "Pi is exactly three!"
}
```

<hr />

`DELETE /settings/value?name=platform-common&key=pi`

Requires an admin token.  DELETE methods cannot accept a body and require parameters.

Response:

```
{
    "success": true,
    "message": "'platform-common.pi' removed."
}
```

## Populating the Portal

So long as you've updated platform-common to 1.1.43+, you can use the baked-in models to navigate all of the settings rather than deal with API requests to read every section.  In order to discourage misuse, however, deletions and updates are not provided here; those will still require separate calls.

DC2Service is automatically provided to every controller so long as you haven't disabled it in PlatformOptions.  In order to get all of the available sections, see the below pseudocode example:

```
foreach (Section section in _dc2Service.GetAdminData())
{
    section.Name;                                   // Equivalent to the CI RUMBLE_COMPONENT
    section.FriendlyName;                           // Used for creating a title; e.g. "Mail" over "mailbox-service"
    
    foreach (string key in section.Data.Keys)       // Navigate over all of the settings in the current section
    {
        SettingsValue setting = section.Data[key];
        object value = setting.Value;
        string comment = setting.Comment;
    }
}

```

TODO: Provide information on the service registration data

## Viewing Diffs

To better facilitate environment promotion and comparison, Dynamic Config now has the ability to compare environment values against each other.  This allows config maintainers to more easily see what is different and highlights missing keys that might have been simple oversights.

Particularly when keys are missing in other environments, maintainers are encouraged to either delete the keys if they're no longer necessary or add the missing keys - even if it means an empty value and a comment explaining it.

To view the diffs, make a call with an admin token to:

```
PATCH /config/diff

{
    // Optional; if specified, you will only see keys from the specified service.
    "filter": "player-service", 

    "environments": [
        "https://dev.nonprod.tower.cdrentertainment.com/",
        ...
    ]
}
```

`filter` is an optional value.  Use your service name, as specified by the `Audience` enum (or what the keys start with); e.g. `player-service`.  However, this is mostly for Postman use to narrow down data; once implemented on Portal, the site can filter data on its own.

`environments` can be any number of urls to compare with.  An invalid URL will _not_ cause the endpoint to return an error, but rather list the failed requests in a `warnings` array in the response.

#### Sample response:
```
{
    "diff": [
        {
            "key": "player-service.confirmationFailurePage",
            "data": [
                {
                    "environment": "https://dev.nonprod.tower.cdrentertainment.com/",
                    "value": "https://stage-a.eng.towersandtitans.com/email/failure/{reason}"
                },
                {
                    "environment": "https://stage-a.nonprod.tower.cdrentertainment.com/",
                    "value": "https://stage-a.eng.towersandtitans.com/email/failure/{reason}"
                },
                {
                    "environment": "https://stage-b.nonprod.tower.cdrentertainment.com/",
                    "value": "https://stage-b.eng.towersandtitans.com/email/failure/{reason}"
                }
            ]
        },
        {
            "key": "player-service.confirmationSuccessPage",
            "data": [
                {
                    "environment": "https://dev.nonprod.tower.cdrentertainment.com/",
                    "value": "https://stage-a.eng.towersandtitans.com/email/success"
                },
                {
                    "environment": "https://stage-a.nonprod.tower.cdrentertainment.com/",
                    "value": "https://stage-a.eng.towersandtitans.com/email/success"
                },
                {
                    "environment": "https://stage-b.nonprod.tower.cdrentertainment.com/",
                    "value": "https://stage-b.eng.towersandtitans.com/email/success"
                }
            ]
        },
        {
            "key": "player-service.delayWelcomeEmail",
            "data": [
                {
                    "environment": "https://dev.nonprod.tower.cdrentertainment.com/",
                    "value": "86400"
                },
                {
                    "environment": "https://stage-a.nonprod.tower.cdrentertainment.com/",
                    "value": "86400"
                },
                {
                    "environment": "https://stage-b.nonprod.tower.cdrentertainment.com/",
                    "value": "300"
                }
            ]
        }
    ],
    "warnings": [
        "Unable to retrieve config at 'https://platform-wrong.prod.tower.rumblegames.com/'."
    ]
}
```

As a sidenote, while this endpoint does require an admin token, there's a reason it's not in the AdminController: the same endpoint is used by the service to collect the config from each environment.  Since an admin token in one environment is not valid in another, Dynamic Config instead authenticates the request with a secret shared across all instances of the service.

## Future Updates, Optimizations, and Nice-to-Haves

* Right now, updating a value can take a very long time, as the registered services have to individually be notified before the endpoint returns.  A new microservice should be created which will notify all registered services on a background thread instead.

## Troubleshooting