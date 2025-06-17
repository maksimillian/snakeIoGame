# Datadog Agent + Node App Example


This template deploys a DataDog Agent from the official Docker image as well as a Node Express App with two endpoints that forward metrics and logs to the agent.

[![Deploy on Railway](https://railway.app/button.svg)](https://railway.app/template/saGmYG)

## DataDog Agent

Built from the [Datadog Agent Docker Image](https://hub.docker.com/r/datadog/agent) 

Configuration files -
- `syslog.yaml` - Configures the Agent to accept syslogs over port 514 forwarded from the node app
- `datadog.yaml` - Configures the Agent to forward logs to DataDog over HTTP (default is UDP which is not supported in Railway)

## Node App

A very simple express server with two endpoints.

Libraries - 

- [Winston](https://www.npmjs.com/package/winston) & [winston-syslog](https://www.npmjs.com/package/winston-syslog) - Used for forwarding logs to the agent
- [hot-shots](https://www.npmjs.com/package/hot-shots) - Used for forwarding metrics to the agent
