CREATE OR REPLACE PROCEDURE create_material_order_with_items(
    IN p_supplier_id INT,
    IN p_items JSONB,
    OUT p_created_order_id INT
) LANGUAGE plpgsql AS $$
DECLARE
    v_order_id INT;
    item_record RECORD;
BEGIN
    IF NOT EXISTS (SELECT 1 FROM material_suppliers WHERE supplier_id = p_supplier_id) THEN
        RAISE EXCEPTION 'Supplier ID % does not exist', p_supplier_id;
    END IF;

    INSERT INTO material_orders (supplier_id, ordered_at, received_at)
    VALUES (p_supplier_id, NOW(), NULL)
    RETURNING order_id INTO v_order_id; 

    IF jsonb_array_length(p_items) = 0 THEN
        RAISE EXCEPTION 'Order must contain at least one item.';
    END IF;

    FOR item_record IN
        SELECT * FROM jsonb_to_recordset(p_items) AS x(material_id INT, amount INT)
    LOOP
        IF item_record.material_id IS NULL OR NOT EXISTS (SELECT 1 FROM materials WHERE material_id = item_record.material_id) THEN
            RAISE EXCEPTION 'Invalid Material ID % provided in order items', item_record.material_id;
        END IF;

        IF item_record.amount IS NULL OR item_record.amount <= 0 THEN
            RAISE EXCEPTION 'Amount for material ID % must be a positive integer', item_record.material_id;
        END IF;

        INSERT INTO material_order_items (material_id, amount, order_id)
        VALUES (item_record.material_id, item_record.amount, v_order_id);
    END LOOP;

    p_created_order_id := v_order_id;

EXCEPTION
    WHEN others THEN
        RAISE EXCEPTION 'Error creating material order: %', SQLERRM;

END;
$$;