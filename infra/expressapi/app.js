const express = require('express');
const app = express();

const StatsD = require('hot-shots');
const { createLogger, format, transports } = require('winston');
require('winston-syslog').Syslog;
const port = process.env.PORT || 3000;

// Configure the StatsD client
const statsdClient = new StatsD({
  host: process.env.DD_AGENT_HOST,
  port: process.env.DD_AGENT_STATSD_PORT,
  protocol: 'udp',
  cacheDns: true,
  udpSocketOptions: {
    type: 'udp6',
    reuseAddr: true,
    ipv6Only: true,
  },
});

// Configure Winston logger
const logger = createLogger({
  level: 'info',
  exitOnError: false,
  format: format.json(),
  transports: [
    new transports.Syslog({
      host: process.env.DD_AGENT_HOST,
      port: process.env.DD_AGENT_SYSLOG_PORT,
      protocol: 'udp6',
      format: format.json(),
      app_name: 'node-app',
    }),
  ],
});

app.get('/', (req, res) => {
  // Increment a counter for the root path
  statsdClient.increment('data_dog_example.homepage.hits');
  statsdClient.gauge('data_dog_example.homepage.hits', 124);

  // forward logs from root path
  logger.info('Root route was accessed');

  res.send('Hello World!');
});

app.get('/test', (req, res) => {
  // Increment a counter for the test path
  statsdClient.increment('data_dog_example.testpage.hits');

  // forward logs from test path
  logger.info('Test route was accessed');

  res.send('This is the test endpoint!');
});

app.listen(port, () => {
  console.log(`Example app listening at port ${port}`);
});
