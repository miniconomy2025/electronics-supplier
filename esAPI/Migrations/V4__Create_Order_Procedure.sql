CREATE OR REPLACE PROCEDURE create_material_order(
    IN p_supplier_id INT,
    IN p_material_id INT,
    IN p_amount INT,
    IN p_current_day NUMERIC(1000, 2), -- The simulation time
    OUT p_created_order_id INT
) LANGUAGE plpgsql AS $$
BEGIN
    -- Validate supplier exists in the companies table
    IF NOT EXISTS (SELECT 1 FROM companies WHERE company_id = p_supplier_id) THEN
        RAISE EXCEPTION 'Supplier (Company ID %) does not exist', p_supplier_id;
    END IF;

    -- Validate material
    IF NOT EXISTS (SELECT 1 FROM materials WHERE material_id = p_material_id) THEN
        RAISE EXCEPTION 'Material ID % does not exist', p_material_id;
    END IF;

    -- Create order with default status 'PENDING' (ID 1)
    INSERT INTO material_orders (supplier_id, material_id, remaining_amount, order_status, ordered_at)
    VALUES (p_supplier_id, p_material_id, p_amount, 1, p_current_day)
    RETURNING order_id INTO p_created_order_id;

EXCEPTION
    WHEN others THEN
        RAISE EXCEPTION 'Error creating material order: %', SQLERRM;
END;
$$;