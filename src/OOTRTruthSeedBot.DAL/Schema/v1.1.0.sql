CREATE TABLE version (
	current TEXT NOT NULL,
	CONSTRAINT pk_version PRIMARY KEY (current)
) STRICT;

CREATE TABLE seed (
	id INTEGER NOT NULL CONSTRAINT pk_seed PRIMARY KEY,
	creator_id TEXT NOT NULL,
	creation_date INTEGER NOT NULL,
	unlocked_date INTEGER NULL,
	state INTEGER NOT NULL CONSTRAINT df_state DEFAULT (0)
) STRICT;

CREATE TABLE restream_notif (
	guid TEXT NOT NULL PRIMARY KEY,
	sent_date INTEGER NOT NULL
) STRICT;

INSERT INTO version VALUES ('1.1.0');