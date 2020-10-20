DROP TABLE IF EXISTS [TestClasses];

CREATE TABLE [TestClasses] (
	[Id] TEXT NOT NULL,
	[Flag] INTEGER NULL,
	[Value] TEXT NULL,
	[Number] INTEGER NULL
);


INSERT INTO TestClasses (Id, Flag, Value, Number)
VALUES
	('a', 0, 'value-a', 0),
	('b', 1, 'value-b', 1),
	('c', 0, 'value-c', 2),
	('d', 1, 'value-d', 3),
	('e', 0, 'value-e', 4),
	('f', 1, 'value-f', 5),
	('g', 0, 'value-g', 6),
	('todelete', 0, 'value-to-delete', 7);