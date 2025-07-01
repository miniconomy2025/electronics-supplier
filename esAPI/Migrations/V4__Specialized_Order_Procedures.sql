CREATE OR REPLACE PROCEDURE complete_material_order(
    IN p_order_id INT
) LANGUAGE plpgsql AS $$
DECLARE
    rec RECORD;
BEGIN

    IF p_order_id IS NULL OR NOT EXISTS (SELECT 1 FROM material_orders WHERE order_id = p_order_id) THEN
        RAISE EXCEPTION 'Material order ID % does not exist', p_order_id;
    END IF;

    IF (SELECT received_at FROM material_orders WHERE order_id = p_order_id) IS NOT NULL THEN
        RAISE EXCEPTION 'Material order % is already completed', p_order_id;
    END IF;

    -- Mark order as received
    UPDATE material_orders SET received_at = NOW() WHERE order_id = p_order_id;

    -- For each item, add to supplies
    FOR rec IN SELECT material_id, amount FROM material_order_items WHERE order_id = p_order_id LOOP
        INSERT INTO supplies (material_id, received_at, processed_at)
        SELECT rec.material_id, NOW(), NULL FROM generate_series(1, rec.amount);
    END LOOP;

    RAISE NOTICE 'Material order % completed and supplies updated', p_order_id;

EXCEPTION
    WHEN others THEN
        RAISE EXCEPTION 'Error completing material order: %', SQLERRM;

END;
$$;

CREATE OR REPLACE PROCEDURE complete_machine_order(
    IN p_order_id INT,
    IN p_purchase_price FLOAT
) LANGUAGE plpgsql AS $$
DECLARE
    v_supplier_id INT;
    v_ordered_at TIMESTAMPTZ;
BEGIN

    IF p_order_id IS NULL OR NOT EXISTS (SELECT 1 FROM machine_orders WHERE order_id = p_order_id) THEN
        RAISE EXCEPTION 'Machine order ID % does not exist', p_order_id;
    END IF;

    IF (SELECT received_at FROM machine_orders WHERE order_id = p_order_id) IS NOT NULL THEN
        RAISE EXCEPTION 'Machine order % is already completed', p_order_id;
    END IF;

    IF p_purchase_price IS NULL OR p_purchase_price <= 0 THEN
        RAISE EXCEPTION 'Purchase price must be greater than 0';
    END IF;

    SELECT supplier_id, ordered_at INTO v_supplier_id, v_ordered_at FROM machine_orders WHERE order_id = p_order_id;

    -- Mark order as received and store purchase price
    UPDATE machine_orders SET received_at = NOW() WHERE order_id = p_order_id;

    -- Insert new machine
    INSERT INTO machines (status, purchase_price, purchased_at) VALUES ('WORKING', p_purchase_price, NOW());

    RAISE NOTICE 'Machine order % completed and machine added', p_order_id;

EXCEPTION
    WHEN others THEN
        RAISE EXCEPTION 'Error completing machine order: %', SQLERRM;

END;
$$;

CREATE OR REPLACE PROCEDURE process_electronics_order(
    IN p_order_id INT
) LANGUAGE plpgsql AS $$
DECLARE
    v_amount INT;
    v_count INT;
BEGIN

    IF p_order_id IS NULL OR NOT EXISTS (SELECT 1 FROM electronics_orders WHERE order_id = p_order_id) THEN
        RAISE EXCEPTION 'Electronics order ID % does not exist', p_order_id;
    END IF;

    IF (SELECT processed_at FROM electronics_orders WHERE order_id = p_order_id) IS NOT NULL THEN
        RAISE EXCEPTION 'Electronics order % is already processed', p_order_id;
    END IF;

    SELECT amount INTO v_amount FROM electronics_orders WHERE order_id = p_order_id;

    -- Mark order as processed
    UPDATE electronics_orders SET processed_at = NOW() WHERE order_id = p_order_id;

    -- Mark unsold electronics as sold
    UPDATE electronics SET sold_at = NOW()
    WHERE electronic_id IN (
        SELECT electronic_id FROM electronics WHERE sold_at IS NULL ORDER BY produced_at LIMIT v_amount
    );
    GET DIAGNOSTICS v_count = ROW_COUNT;

    IF v_count < v_amount THEN
        RAISE NOTICE 'Only % out of % electronics were marked as sold (not enough unsold stock)', v_count, v_amount;
    END IF;

    RAISE NOTICE 'Electronics order % processed and electronics sold', p_order_id;

EXCEPTION
    WHEN others THEN
        RAISE EXCEPTION 'Error processing electronics order: %', SQLERRM;
        
END;
$$; 