package migrations

import (
	_ "embed"
)

//go:embed 001_init_notification_schema.sql
var InitSchemaSQL string
