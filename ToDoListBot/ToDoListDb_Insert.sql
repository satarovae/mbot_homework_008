-- Тестовые данные

INSERT INTO "ToDoUser" ("UserId", "TelegramUserId", "TelegramUserName", "RegisteredAt") VALUES
    ('11111111-1111-1111-1111-111111111111', 100000001, 'test_user_1', NOW()),
    ('22222222-2222-2222-2222-222222222222', 100000002, 'test_user_2', NOW());

INSERT INTO "ToDoList" ("Id", "Name", "UserId", "CreatedAt") VALUES
    ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'Работа', '11111111-1111-1111-1111-111111111111', NOW()),
    ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 'Дом',    '22222222-2222-2222-2222-222222222222', NOW());

INSERT INTO "ToDoItem" ("Id", "UserId", "ListId", "Name", "CreatedAt", "State", "StateChangedAt", "Deadline") VALUES
    ('c1111111-0000-0000-0000-000000000001', '11111111-1111-1111-1111-111111111111', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'Написать отчет',  NOW(), 0, NULL,  NOW() + INTERVAL '3 day'),
    ('c1111111-0000-0000-0000-000000000002', '11111111-1111-1111-1111-111111111111', NULL,                                   'Купить хлеб',     NOW(), 1, NOW(), NOW() + INTERVAL '1 day'),
    ('c2222222-0000-0000-0000-000000000001', '22222222-2222-2222-2222-222222222222', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 'Помыть посуду',   NOW(), 0, NULL,  NOW() + INTERVAL '2 day');
