# DMS Resilience Strategies

Data Management Service employs a couple resilience patterns when the
application interacts with the database and unhandled exceptions occur.

## Database Retry

When database commands are executed and exceptions are caught, DMS will retry
the command. The number of retries and the time between them is determined by
whether the exception is deemed to be transient or not.

### Transient Exceptions

The `NpgsqlException` class has a boolean property `IsTransient` which specifies
whether retrying the operation could succeed
([documentation](https://www.npgsql.org/doc/api/Npgsql.NpgsqlException.html#Npgsql_NpgsqlException_IsTransient)).
One possible cause for a transient exception would be a serialization failure
due to concurrent transactions trying to modify the same record. See
[Serialization Failure
Handling](https://www.postgresql.org/docs/current/mvcc-serialization-failure-handling.html).
When a transient exception is caught, DMS employs a retry strategy with the
following parameters:

* Delay: 500 ms
* Backoff Type: Exponential (500 ms, 1000 ms, 2000 ms, 4000 ms)
* Maximum Retry Attempts: 4

### Non-transient Exceptions

All other non-transient but unexpected exceptions will still be retried, only
fewer times and with a longer delay as there is less probability of eventual
success. DMS's non-transient retry strategy uses the following parameters:

* Delay: 1000 ms
* Backoff Type: Exponential (1000 ms, 2000 ms)
* Maximum Retry Attempts: 2

## Backend Circuit Breaker

When the DMS backend exhausts all retry attempts it will return a result of type
`UnknownFaiure` which will ultimately result in an HTTP 500 error response to
the client. Application administrators may wish to employ a circuit breaker
resilience strategy when the number of such unknown failures exceeds a threshold
for an extended period of time. In such a scenario, an entire backend service is
likely down and repeated requests would be unhelpful and incur unnecessary cost
or other strain on the network. DMS has the following configuration settings for
this backend circuit breaker:

* `FailureRatio`: The ratio of failed backend requests as defined by
  `UnknownFailure` returned type expressed as a double between 0 and 1.
* `SamplingDurationSeconds`: The rolling time span over which to sample for
  failure ratio.
* `MinimumThroughput`: The minimum number of responses that must exist in the
  time span before the ratio is calculated and acted upon. The minimum value
  here is 2.
* `BreakDurationSeconds`: How long to break the circuit (stop making backend
  requests) and just return the HTTP 500 error.

## References

* [Polly Retry resilience strategy](https://www.pollydocs.org/strategies/retry.html)
* [Polly Circuit breaker resilience strategy](https://www.pollydocs.org/strategies/circuit-breaker.html)
