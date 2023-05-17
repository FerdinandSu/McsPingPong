# Tool for Make Ping-pong in microservice systems

## Build

```bash
docker build .
```

## Env Variables

- APP_URL: request url for ping
- INSTANCE_ID: Id configured for each instance

## APIs

### Ping: /ping

Method: Post

Request:

```json
{
  "ttl": 1, //>=1
  "count": 0,// use 0 for manual ping
  "initial": {
    "id": "string",// whatever you like for initial
    "ip": "string"// whatever you like for initial
  },
  "sender": {
    "id": "string",// whatever you like for initial
    "ip": "string"// whatever you like for initial
  }
}
```

Response:

```json
{
  "count": 1,// = initial TTL
  "initial": {
    "id": "string",
    "ip": "string"
  },
  "lastNode": {
    "id": "string",
    "ip": "string"
  },
  "thisNode": {
    "id": "default", // id last arrived node
    "ip": "172.17.0.2" // last arrived node
  }
}
```

### Batch: /batch

Method: Post

Request:

```json
{
  "ttl": 0,// TTL for each ping
  "batchSize": 0// Number of ping requests to execute
}
```

Response:

```json
{
  "stats": {
    "[Ip,Id]": {
      "count": 0,// Number of response with lastnode=[Ip,Id]
      "ratio": 0 // Ratio of response with lastnode=[Ip,Id]
    }
  },
  "details": [
    {
      "finalNode": { // lastnode from pong
        "id": "string",
        "ip": "string"
      },
      "time": 0 // time used, unit: ms
    }
  ]
}
```
