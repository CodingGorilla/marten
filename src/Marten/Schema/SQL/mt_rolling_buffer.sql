﻿DROP SEQUENCE IF EXISTS {databaseSchema}.mt_rolling_buffer_sequence;
CREATE SEQUENCE {databaseSchema}.mt_rolling_buffer_sequence START WITH 1;

DROP TABLE IF EXISTS {databaseSchema}.mt_rolling_buffer CASCADE;
CREATE TABLE {databaseSchema}.mt_rolling_buffer (
	slot				integer CONSTRAINT pk_mt_rolling_buffer PRIMARY KEY,
	message_id			integer NOT NULL,
	timestamp			timestamp without time zone default (now() at time zone 'utc') NOT NULL,
	event_id			UUID NULL,
	stream_id			UUID NULL,
	reference_count		integer NOT NULL	
);

DROP TABLE IF EXISTS {databaseSchema}.mt_options CASCADE;
CREATE TABLE {databaseSchema}.mt_options (
    buffer_size				integer NOT NULL
);

CREATE OR REPLACE FUNCTION {databaseSchema}.mt_buffer_size() RETURNS integer AS $$
DECLARE
	size integer;
BEGIN
	select buffer_size into size from {databaseSchema}.mt_options LIMIT 1;

	IF size IS NULL THEN
		return 200;
	END IF;

	return size;
END
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION {databaseSchema}.mt_reset_rolling_buffer_size(size int) RETURNS VOID AS $$
DECLARE
	current integer;
BEGIN
	UPDATE {databaseSchema}.mt_options set buffer_size = size;

	IF NOT FOUND THEN
		insert into {databaseSchema}.mt_options (buffer_size) values (size);
	END IF; 

	select count(*) into current from {databaseSchema}.mt_rolling_buffer;
	IF current < size THEN
		perform {databaseSchema}.mt_seed_rolling_buffer();
	ELSE
		delete from {databaseSchema}.mt_rolling_buffer where slot > size;
	END IF;
END	
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION {databaseSchema}.mt_seed_rolling_buffer() RETURNS VOID AS $$
DECLARE
	size integer;
	i integer := 0;
	timestamp timestamp := current_timestamp;
	current integer;
BEGIN
	size := {databaseSchema}.mt_buffer_size();

	SELECT count(*) into i FROM {databaseSchema}.mt_rolling_buffer;

	WHILE i < size LOOP
		insert into {databaseSchema}.mt_rolling_buffer 
			(slot, message_id, timestamp, event_id, stream_id, reference_count)
		values
			(i + 1, 0, timestamp, NULL, NULL, 0);

		i := i + 1;
	END LOOP;


END
$$ LANGUAGE plpgsql;


CREATE OR REPLACE FUNCTION {databaseSchema}.mt_append_rolling_buffer(event UUID, stream UUID) RETURNS int AS $$
DECLARE
	id int := nextval('{databaseSchema}.mt_rolling_buffer_sequence');
	next int;
	next_str varchar;
	size int;
BEGIN
	size := {databaseSchema}.mt_buffer_size();
	next := id % size;

	update {databaseSchema}.mt_rolling_buffer
		SET
			timestamp = current_timestamp,
			message_id = id,
			event_id = event,
			stream_id = stream,
			reference_count = reference_count + 1
		WHERE
			slot = next;

	-- Try again if it's filled up
    IF NOT found THEN
        perform pg_sleep(.100);
		update {databaseSchema}.mt_rolling_buffer
			SET
				timestamp = current_timestamp,
				message_id = id,
				event_id = event,
				stream_id = stream,
				reference_count = reference_count + 1
			WHERE
				slot = next AND reference_count = 0;
	END IF;

	--next_str = to_char(next, '999999999999');

		--perform pg_notify('mt_event_queued', next_str);

	return id;
END
$$ LANGUAGE plpgsql;


CREATE OR REPLACE FUNCTION {databaseSchema}.mt_append_event_with_buffering(stream uuid, stream_type varchar, event_ids uuid[], event_types varchar[], bodies jsonb[]) RETURNS int AS $$
DECLARE
	index int;
	event_id uuid;
	version int;
	id int;
BEGIN

	foreach event_id in ARRAY event_ids
	loop
		select {databaseSchema}.mt_append_rolling_buffer(event_id, stream) into id;
	end loop;


	SELECT {databaseSchema}.mt_append_event(stream, stream_type, event_ids, event_types, bodies) INTO version;

	RETURN version;
END
$$ LANGUAGE plpgsql;
