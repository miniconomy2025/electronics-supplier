CREATE OR REPLACE PROCEDURE create_material_order(
    IN p_supplier_id INT,
    IN p_material_id INT,
    IN p_remaining_amount INT,
    OUT p_created_order_id INT
) LANGUAGE plpgsql AS $$
DECLARE
    v_order_id INT;
BEGIN
    -- Validate supplier
    IF NOT EXISTS (SELECT 1 FROM material_suppliers WHERE supplier_id = p_supplier_id) THEN
        RAISE EXCEPTION 'Supplier ID % does not exist', p_supplier_id;
    END IF;

    -- Validate material
    IF NOT EXISTS (SELECT 1 FROM materials WHERE material_id = p_material_id) THEN
        RAISE EXCEPTION 'Material ID % does not exist', p_material_id;
    END IF;

    -- Validate remaining amount
    IF p_remaining_amount <= 0 THEN
        RAISE EXCEPTION 'Remaining amount must be a positive integer';
    END IF;

    -- Create order
    INSERT INTO material_orders (supplier_id, material_id, remaining_amount, ordered_at, received_at)
    VALUES (p_supplier_id, p_material_id, p_remaining_amount, NOW(), NULL)
    RETURNING order_id INTO v_order_id;

    p_created_order_id := v_order_id;

EXCEPTION
    WHEN others THEN
        RAISE EXCEPTION 'Error creating material order: %', SQLERRM;

END;
$$;
