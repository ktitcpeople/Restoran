-- Удаляет только демо-блюда и демо-категории.
-- Заказы и связанные с ними записи сохраняются: позиции заказов переводятся
-- на новые блюда без приставки «Демо». Новые пользователи и категории не создаются.
-- Выполнить один раз после миграций в базе проекта.

BEGIN;

DO $replace_demo$
DECLARE
    v_branch_id integer;
    v_replaced_count integer := 0;
BEGIN
    FOR v_branch_id IN
        SELECT DISTINCT x."BranchID"
        FROM (
            SELECT m."BranchID"
            FROM public."MenuItems" m
            WHERE m."Name" ILIKE 'Демо%'
               OR m."Category" ILIKE 'Демо%'
            UNION
            SELECT c."BranchID"
            FROM public."MenuCategories" c
            WHERE c."Name" ILIKE 'Демо%'
        ) AS x
    LOOP
        -- Категории не создаём: для новых блюд выбирается существующая категория
        -- филиала, сначала подходящая по смыслу, затем любая недемо-категория.
        IF NOT EXISTS (
            SELECT 1
            FROM public."MenuCategories" c
            WHERE c."BranchID" = v_branch_id
              AND c."Name" NOT ILIKE 'Демо%'
        ) THEN
            RAISE EXCEPTION
                'В филиале % нет существующих обычных категорий. Создайте/загрузите базовые категории через раздел меню и запустите скрипт повторно.',
                v_branch_id;
        END IF;

        -- Предотвращаем повтор PK, если раньше в таблицу вручную вставляли ID.
        PERFORM setval(
            pg_get_serial_sequence('public."MenuItems"', 'MenuItemID'),
            GREATEST(COALESCE((SELECT MAX("MenuItemID") FROM public."MenuItems"), 1), 1),
            EXISTS (SELECT 1 FROM public."MenuItems")
        );

        -- Создаём чистые названия на основе демо-позиций, используя только
        -- уже существующие категории этого филиала.
        WITH demo_items AS (
            SELECT
                m.*,
                regexp_replace(m."Name", '^Демо[[:space:]]*[—-][[:space:]]*', '') AS "CleanName"
            FROM public."MenuItems" m
            WHERE m."BranchID" = v_branch_id
              AND m."Name" ILIKE 'Демо%'
        ),
        categorized AS (
            SELECT
                d.*,
                CASE
                    WHEN d."CleanName" ILIKE 'Борщ%' THEN 'Супы'
                    WHEN d."CleanName" ILIKE 'Салат%' THEN 'Салаты'
                    WHEN d."CleanName" ILIKE 'Медовик%' THEN 'Десерты'
                    WHEN d."CleanName" ILIKE 'Морс%' THEN 'Напитки'
                    ELSE 'Горячее'
                END AS "PreferredCategory"
            FROM demo_items d
        )
        INSERT INTO public."MenuItems"
            ("Name", "Price", "Category", "KitchenStation", "ImageUrl", "ImageData",
             "ImageMimeType", "Description", "Protein", "Fat", "Carbohydrates",
             "Calories", "WeightGrams", "BranchID", "MenuCategoryID", "IsAvailable")
        SELECT
            d."CleanName",
            d."Price",
            c."Name",
            d."KitchenStation",
            d."ImageUrl",
            d."ImageData",
            d."ImageMimeType",
            d."Description",
            d."Protein",
            d."Fat",
            d."Carbohydrates",
            d."Calories",
            d."WeightGrams",
            d."BranchID",
            c."MenuCategoryID",
            d."IsAvailable"
        FROM categorized d
        CROSS JOIN LATERAL (
            SELECT c."MenuCategoryID", c."Name"
            FROM public."MenuCategories" c
            WHERE c."BranchID" = v_branch_id
              AND c."Name" NOT ILIKE 'Демо%'
            ORDER BY
                CASE
                    WHEN c."Name" = d."PreferredCategory" THEN 0
                    WHEN c."Name" = 'Горячее' THEN 1
                    ELSE 2
                END,
                c."MenuCategoryID"
            LIMIT 1
        ) c
        WHERE NOT EXISTS (
            SELECT 1
            FROM public."MenuItems" existing
            WHERE existing."BranchID" = d."BranchID"
              AND existing."Name" = d."CleanName"
              AND existing."Name" NOT ILIKE 'Демо%'
        );

        -- Переносим ссылки позиций заказов на чистые блюда, чтобы сохранить
        -- заказы, оплаты и доставки, связанные с прежними демо-позициями.
        UPDATE public."OrderItems" oi
        SET "MenuItemID" = clean."MenuItemID"
        FROM public."MenuItems" old_item
        JOIN public."MenuItems" clean
          ON clean."BranchID" = old_item."BranchID"
         AND clean."Name" = regexp_replace(old_item."Name", '^Демо[[:space:]]*[—-][[:space:]]*', '')
         AND clean."Name" NOT ILIKE 'Демо%'
        WHERE old_item."BranchID" = v_branch_id
          AND old_item."Name" ILIKE 'Демо%'
          AND oi."MenuItemID" = old_item."MenuItemID";

        -- Если обычное блюдо ссылалось на демо-категорию, назначаем ему
        -- существующую категорию этого филиала, не удаляя само блюдо.
        WITH affected AS (
            SELECT DISTINCT m."MenuItemID", m."BranchID"
            FROM public."MenuItems" m
            LEFT JOIN public."MenuCategories" old_category
              ON old_category."MenuCategoryID" = m."MenuCategoryID"
            WHERE m."BranchID" = v_branch_id
              AND m."Name" NOT ILIKE 'Демо%'
              AND (
                    m."Category" ILIKE 'Демо%'
                    OR old_category."Name" ILIKE 'Демо%'
              )
        ),
        replacements AS (
            SELECT a."MenuItemID", c."MenuCategoryID", c."Name"
            FROM affected a
            CROSS JOIN LATERAL (
                SELECT c."MenuCategoryID", c."Name"
                FROM public."MenuCategories" c
                WHERE c."BranchID" = a."BranchID"
                  AND c."Name" NOT ILIKE 'Демо%'
                ORDER BY CASE WHEN c."Name" = 'Горячее' THEN 0 ELSE 1 END,
                         c."MenuCategoryID"
                LIMIT 1
            ) c
        )
        UPDATE public."MenuItems" m
        SET "Category" = r."Name",
            "MenuCategoryID" = r."MenuCategoryID"
        FROM replacements r
        WHERE m."MenuItemID" = r."MenuItemID";

        -- Удаляем только старые демо-блюда; прочие данные не трогаем.
        DELETE FROM public."MenuItems"
        WHERE "BranchID" = v_branch_id
          AND "Name" ILIKE 'Демо%';

        -- Теперь ссылки переведены, поэтому демо-категории можно удалить.
        DELETE FROM public."MenuCategories"
        WHERE "BranchID" = v_branch_id
          AND "Name" ILIKE 'Демо%';

        v_replaced_count := v_replaced_count + 1;
    END LOOP;

    RAISE NOTICE 'Обработано филиалов с демо-позициями/категориями: %.', v_replaced_count;
END
$replace_demo$;

COMMIT;
