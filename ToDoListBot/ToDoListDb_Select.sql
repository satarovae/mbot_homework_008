-- Запросы для методов IToDoRepository

-- GetAllByUserId
SELECT * FROM "ToDoItem" WHERE "UserId" = '11111111-1111-1111-1111-111111111111';

-- GetActiveByUserId (State = 0 -> Active)
SELECT * FROM "ToDoItem" WHERE "UserId" = '11111111-1111-1111-1111-111111111111' AND "State" = 0;

-- Get
SELECT * FROM "ToDoItem" WHERE "Id" = 'c1111111-0000-0000-0000-000000000001';

-- ExistsByName
SELECT EXISTS (
    SELECT 1 FROM "ToDoItem"
    WHERE "UserId" = '11111111-1111-1111-1111-111111111111' AND LOWER("Name") = LOWER('Купить хлеб')
);

-- CountActive
SELECT COUNT(*) FROM "ToDoItem" WHERE "UserId" = '11111111-1111-1111-1111-111111111111' AND "State" = 0;

-- Find (поиск по префиксу имени)
SELECT * FROM "ToDoItem" WHERE "UserId" = '11111111-1111-1111-1111-111111111111' AND "Name" LIKE 'Куп%';
