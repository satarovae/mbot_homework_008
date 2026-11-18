-- Схема БД для ToDoList бота (ДЗ "Модель базы данных")
-- Создать БД: CREATE DATABASE "ToDoList";

CREATE TABLE IF NOT EXISTS "ToDoUser"
(
    "UserId"           UUID         NOT NULL,
    "TelegramUserId"   BIGINT       NOT NULL,
    "TelegramUserName" VARCHAR(255) NOT NULL,
    "RegisteredAt"     TIMESTAMPTZ  NOT NULL,
    CONSTRAINT "PK_ToDoUser" PRIMARY KEY ("UserId")
);

CREATE TABLE IF NOT EXISTS "ToDoList"
(
    "Id"        UUID        NOT NULL,
    "Name"      VARCHAR(10) NOT NULL,
    "UserId"    UUID        NOT NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL,
    CONSTRAINT "PK_ToDoList" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ToDoList_ToDoUser" FOREIGN KEY ("UserId") REFERENCES "ToDoUser" ("UserId")
);

CREATE TABLE IF NOT EXISTS "ToDoItem"
(
    "Id"             UUID         NOT NULL,
    "UserId"         UUID         NOT NULL,
    "ListId"         UUID         NULL,
    "Name"           VARCHAR(255) NOT NULL,
    "CreatedAt"      TIMESTAMPTZ  NOT NULL,
    "State"          INT          NOT NULL,
    "StateChangedAt" TIMESTAMPTZ  NULL,
    "Deadline"       TIMESTAMPTZ  NOT NULL,
    CONSTRAINT "PK_ToDoItem" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ToDoItem_ToDoUser" FOREIGN KEY ("UserId") REFERENCES "ToDoUser" ("UserId"),
    CONSTRAINT "FK_ToDoItem_ToDoList" FOREIGN KEY ("ListId") REFERENCES "ToDoList" ("Id")
);

-- Индексы для внешних ключей
CREATE INDEX IF NOT EXISTS "IX_ToDoList_UserId" ON "ToDoList" ("UserId");
CREATE INDEX IF NOT EXISTS "IX_ToDoItem_UserId" ON "ToDoItem" ("UserId");
CREATE INDEX IF NOT EXISTS "IX_ToDoItem_ListId" ON "ToDoItem" ("ListId");

-- Уникальный индекс для TelegramUserId
CREATE UNIQUE INDEX IF NOT EXISTS "UX_ToDoUser_TelegramUserId" ON "ToDoUser" ("TelegramUserId");

-- ДЗ "Нотификации"
CREATE TABLE IF NOT EXISTS "Notification"
(
    "Id"          UUID         NOT NULL,
    "UserId"      UUID         NOT NULL,
    "Type"        VARCHAR(255) NOT NULL,
    "Text"        TEXT         NOT NULL,
    "ScheduledAt" TIMESTAMPTZ  NOT NULL,
    "IsNotified"  BOOLEAN      NOT NULL DEFAULT FALSE,
    "NotifiedAt"  TIMESTAMPTZ  NULL,
    CONSTRAINT "PK_Notification" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Notification_ToDoUser" FOREIGN KEY ("UserId") REFERENCES "ToDoUser" ("UserId")
);

CREATE INDEX IF NOT EXISTS "IX_Notification_UserId" ON "Notification" ("UserId");
