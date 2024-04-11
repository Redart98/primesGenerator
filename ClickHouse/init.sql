USE alfn;

CREATE TABLE primes
(
	created_at DateTime64(9),
	produced_at DateTime64(9),
	creator LowCardinality(String),
	prime Decimal(40)
) ENGINE = MergeTree ORDER BY created_at;

CREATE TABLE primes_queue
(
	Number Decimal(40),
	GeneratedAt DateTime64(9)
) ENGINE = Kafka('kafka:29092', 'test', 'clickhouse',
            'JSONEachRow') settings kafka_thread_per_consumer = 0, kafka_num_consumers = 1;

CREATE MATERIALIZED VIEW primes_mv TO primes AS
SELECT GeneratedAt as created_at, _timestamp as produced_at, 'bozhenov' as creator, Number as prime
FROM primes_queue;