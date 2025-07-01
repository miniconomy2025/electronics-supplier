CREATE OR REPLACE PROCEDURE add_machine(
    IN p_purchase_price FLOAT,
    IN p_purchased_at TIMESTAMPTZ DEFAULT NOW(),
    IN p_material_ratios JSON
) LANGUAGE plpgsql AS $$
DECLARE
    v_machine_id INT;
    v_key VARCHAR(8);
    v_ratio INT;
    v_material_id INT;
    v_count INT;
    v_seen_names VARCHAR(8)[] := ARRAY[]::VARCHAR(8)[];
BEGIN
    
    IF p_purchase_price IS NULL OR p_purchase_price <= 0 THEN
        RAISE EXCEPTION 'Purchase price must be greater than 0';
    END IF;

    IF p_purchased_at IS NOT NULL AND p_purchased_at > NOW() THEN
        RAISE EXCEPTION 'Purchased at must be in the past';
    END IF;
    
    SELECT COUNT(*) INTO v_count FROM json_object_keys(p_material_ratios);
    IF v_count < 2 OR v_count > 3 THEN
        RAISE EXCEPTION 'A machine must have at least 2 materials';
    END IF;
    
    INSERT INTO machines (purchase_price, purchased_at) VALUES (p_purchase_price, p_purchased_at) RETURNING machine_id INTO v_machine_id;
    
    FOR v_key IN SELECT json_object_keys(p_material_ratios) LOOP
        IF v_key = ANY(v_seen_names) THEN
            RAISE EXCEPTION 'Duplicate material name: %', v_key;
        END IF;
        v_seen_names := array_append(v_seen_names, v_key);
        
        v_ratio := (p_material_ratios ->> v_key)::INT;
        IF v_ratio IS NULL OR v_ratio <= 0 THEN
            RAISE EXCEPTION 'Ratio for material % must be a positive integer', v_key;
        END IF;
        
        SELECT material_id INTO v_material_id FROM materials WHERE material_name = v_key;
        IF v_material_id IS NULL THEN
            RAISE EXCEPTION 'Material name % does not exist', v_key;
        END IF;
        
        INSERT INTO machine_ratios (material_id, ratio, machine_id) VALUES (v_material_id, v_ratio, v_machine_id);
    END LOOP;

    RAISE NOTICE 'Machine and ratios added successfully';

EXCEPTION
    WHEN others THEN
        RAISE EXCEPTION 'Error adding machine: %', SQLERRM;

END;
$$;

CREATE OR REPLACE PROCEDURE add_material(
    IN p_material_name VARCHAR(8)
) LANGUAGE plpgsql AS $$
BEGIN

    p_material_name := TRIM(p_material_name);
    
    IF p_material_name IS NULL OR LENGTH(p_material_name) = 0 THEN
        RAISE EXCEPTION 'Material name must not be empty';
    END IF;

    INSERT INTO materials (material_name)
    VALUES (p_material_name);

    RAISE NOTICE 'Material added successfully';

EXCEPTION
    WHEN unique_violation THEN
        RAISE EXCEPTION 'Material name must be unique';
    WHEN others THEN
        RAISE EXCEPTION 'Error adding material: %', SQLERRM;
END;
$$;

CREATE OR REPLACE PROCEDURE add_supply(
    IN p_material_id INT,
    IN p_received_at TIMESTAMPTZ,
    IN p_processed_at TIMESTAMPTZ DEFAULT NULL
) LANGUAGE plpgsql AS $$
BEGIN

    IF p_material_id IS NULL THEN
        RAISE EXCEPTION 'Material ID must not be null';
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM materials WHERE material_id = p_material_id) THEN
        RAISE EXCEPTION 'Material ID % does not exist', p_material_id;
    END IF;
    
    IF p_received_at IS NULL THEN
        RAISE EXCEPTION 'Received at must not be null';
    END IF;

    IF p_received_at > NOW() THEN
        RAISE EXCEPTION 'Received at must be in the past';
    END IF;

    INSERT INTO supplies (material_id, received_at, processed_at)
    VALUES (p_material_id, p_received_at, p_processed_at);

    RAISE NOTICE 'Supply added successfully';

EXCEPTION
    WHEN others THEN
        RAISE EXCEPTION 'Error adding supply: %', SQLERRM;
        
END;
$$;

CREATE OR REPLACE PROCEDURE add_machine_ratio(
    IN p_material_id INT,
    IN p_ratio INT,
    IN p_machine_id INT
) LANGUAGE plpgsql AS $$
BEGIN

    IF p_material_id IS NULL OR NOT EXISTS (SELECT 1 FROM materials WHERE material_id = p_material_id) THEN
        RAISE EXCEPTION 'Material ID % does not exist', p_material_id;
    END IF;

    IF p_machine_id IS NULL OR NOT EXISTS (SELECT 1 FROM machines WHERE machine_id = p_machine_id) THEN
        RAISE EXCEPTION 'Machine ID % does not exist', p_machine_id;
    END IF;
    
    IF p_ratio IS NULL OR p_ratio <= 0 THEN
        RAISE EXCEPTION 'Ratio must be a positive integer';
    END IF;

    INSERT INTO machine_ratios (material_id, ratio, machine_id)
    VALUES (p_material_id, p_ratio, p_machine_id);

    RAISE NOTICE 'Machine ratio added successfully';

EXCEPTION
    WHEN others THEN
        RAISE EXCEPTION 'Error adding machine ratio: %', SQLERRM;
END;
$$;

CREATE OR REPLACE PROCEDURE add_machine_order(
    IN p_supplier_id INT,
    IN p_ordered_at TIMESTAMPTZ,
    IN p_received_at TIMESTAMPTZ DEFAULT NULL
) LANGUAGE plpgsql AS $$
BEGIN

    IF p_supplier_id IS NULL OR NOT EXISTS (SELECT 1 FROM material_suppliers WHERE supplier_id = p_supplier_id) THEN
        RAISE EXCEPTION 'Supplier ID % does not exist', p_supplier_id;
    END IF;

    IF p_ordered_at IS NULL OR p_ordered_at > NOW() THEN
        RAISE EXCEPTION 'Ordered at must be a valid past timestamp';
    END IF;

    IF p_received_at IS NOT NULL AND p_received_at < p_ordered_at THEN
        RAISE EXCEPTION 'Received at cannot be before ordered at';
    END IF;

    INSERT INTO machine_orders (supplier_id, ordered_at, received_at)
    VALUES (p_supplier_id, p_ordered_at, p_received_at);

    RAISE NOTICE 'Machine order added successfully';

    EXCEPTION
        WHEN others THEN
            RAISE EXCEPTION 'Error adding machine order: %', SQLERRM;
            
END;
$$;

CREATE OR REPLACE PROCEDURE add_material_supplier(
    IN p_supplier_name VARCHAR(16)
) LANGUAGE plpgsql AS $$
BEGIN

    p_supplier_name := TRIM(p_supplier_name);

    IF p_supplier_name IS NULL OR LENGTH(p_supplier_name) = 0 THEN
        RAISE EXCEPTION 'Supplier name must not be empty';
    END IF;

    INSERT INTO material_suppliers (supplier_name)
    VALUES (p_supplier_name);
    
    RAISE NOTICE 'Material supplier added successfully';

    EXCEPTION
        WHEN others THEN
            RAISE EXCEPTION 'Error adding material supplier: %', SQLERRM;            

END;
$$;

CREATE OR REPLACE PROCEDURE add_material_order(
    IN p_supplier_id INT,
    IN p_ordered_at TIMESTAMPTZ,
    IN p_received_at TIMESTAMPTZ DEFAULT NULL
) LANGUAGE plpgsql AS $$
BEGIN

    IF p_supplier_id IS NULL OR NOT EXISTS (SELECT 1 FROM material_suppliers WHERE supplier_id = p_supplier_id) THEN
        RAISE EXCEPTION 'Supplier ID % does not exist', p_supplier_id;
    END IF;

    IF p_ordered_at IS NULL OR p_ordered_at > NOW() THEN
        RAISE EXCEPTION 'Ordered at must be a valid past timestamp';
    END IF;    

    IF p_received_at IS NOT NULL AND p_received_at < p_ordered_at THEN
        RAISE EXCEPTION 'Received at cannot be before ordered at';
    END IF;    

    INSERT INTO material_orders (supplier_id, ordered_at, received_at)
    VALUES (p_supplier_id, p_ordered_at, p_received_at);    

    RAISE NOTICE 'Material order added successfully';    

    EXCEPTION
        WHEN others THEN
            RAISE EXCEPTION 'Error adding material order: %', SQLERRM;

END;
$$;

CREATE OR REPLACE PROCEDURE add_material_order_item(
    IN p_material_id INT,
    IN p_amount INT,
    IN p_order_id INT
) LANGUAGE plpgsql AS $$
BEGIN

    IF p_material_id IS NULL OR NOT EXISTS (SELECT 1 FROM materials WHERE material_id = p_material_id) THEN
        RAISE EXCEPTION 'Material ID % does not exist', p_material_id;
    END IF;

    IF p_order_id IS NULL OR NOT EXISTS (SELECT 1 FROM material_orders WHERE order_id = p_order_id) THEN
        RAISE EXCEPTION 'Order ID % does not exist', p_order_id;
    END IF;

    IF p_amount IS NULL OR p_amount <= 0 THEN
        RAISE EXCEPTION 'Amount must be a positive integer';
    END IF;

    INSERT INTO material_order_items (material_id, amount, order_id)
    VALUES (p_material_id, p_amount, p_order_id);

    RAISE NOTICE 'Material order item added successfully';

    EXCEPTION
        WHEN others THEN
            RAISE EXCEPTION 'Error adding material order item: %', SQLERRM;

END;
$$;

CREATE OR REPLACE PROCEDURE add_phone_manufacturer(
    IN p_manufacturer_name VARCHAR(8)
) LANGUAGE plpgsql AS $$
BEGIN

    p_manufacturer_name := TRIM(p_manufacturer_name);

    IF p_manufacturer_name IS NULL OR LENGTH(p_manufacturer_name) = 0 THEN
        RAISE EXCEPTION 'Manufacturer name must not be empty';
    END IF;

    INSERT INTO phone_manufacturers (manufacturer_name)
    VALUES (p_manufacturer_name);

    RAISE NOTICE 'Phone manufacturer added successfully';

    EXCEPTION
        WHEN others THEN
            RAISE EXCEPTION 'Error adding phone manufacturer: %', SQLERRM;

END;
$$;

CREATE OR REPLACE PROCEDURE add_electronic(
    IN p_produced_at TIMESTAMPTZ,
    IN p_sold_at TIMESTAMPTZ DEFAULT NULL
) LANGUAGE plpgsql AS $$
BEGIN

    IF p_produced_at IS NULL OR p_produced_at > NOW() THEN
        RAISE EXCEPTION 'Produced at must be a valid past timestamp';
    END IF;

    IF p_sold_at IS NOT NULL AND p_sold_at < p_produced_at THEN
        RAISE EXCEPTION 'Sold at cannot be before produced at';
    END IF;

    INSERT INTO electronics (produced_at, sold_at)
    VALUES (p_produced_at, p_sold_at);

    RAISE NOTICE 'Electronic added successfully';

    EXCEPTION
        WHEN others THEN
            RAISE EXCEPTION 'Error adding electronic: %', SQLERRM;
END;
$$;

CREATE OR REPLACE PROCEDURE add_electronics_order(
    IN p_manufacturer_id INT,
    IN p_amount INT,
    IN p_ordered_at TIMESTAMPTZ,
    IN p_processed_at TIMESTAMPTZ DEFAULT NULL
) LANGUAGE plpgsql AS $$
BEGIN

    IF p_manufacturer_id IS NULL OR NOT EXISTS (SELECT 1 FROM phone_manufacturers WHERE manufacturer_id = p_manufacturer_id) THEN
        RAISE EXCEPTION 'Manufacturer ID % does not exist', p_manufacturer_id;
    END IF;

    IF p_amount IS NULL OR p_amount <= 0 THEN
        RAISE EXCEPTION 'Amount must be a positive integer';
    END IF;

    IF p_ordered_at IS NULL OR p_ordered_at > NOW() THEN
        RAISE EXCEPTION 'Ordered at must be a valid past timestamp';
    END IF;

    IF p_processed_at IS NOT NULL AND p_processed_at < p_ordered_at THEN
        RAISE EXCEPTION 'Processed at cannot be before ordered at';
    END IF;

    INSERT INTO electronics_orders (manufacturer_id, amount, ordered_at, processed_at)
    VALUES (p_manufacturer_id, p_amount, p_ordered_at, p_processed_at);

    RAISE NOTICE 'Electronics order added successfully';

    EXCEPTION
        WHEN others THEN
            RAISE EXCEPTION 'Error adding electronics order: %', SQLERRM;
END;
$$;

CREATE OR REPLACE PROCEDURE add_lookup_value(
    IN p_electronics_price FLOAT
) LANGUAGE plpgsql AS $$
BEGIN

    IF p_electronics_price IS NULL OR p_electronics_price <= 0 THEN
        RAISE EXCEPTION 'Electronics price must be a positive number';
    END IF;

    INSERT INTO lookup_values (electronics_price)
    VALUES (p_electronics_price);

    RAISE NOTICE 'Lookup value added successfully';

    EXCEPTION
        WHEN others THEN
            RAISE EXCEPTION 'Error adding lookup value: %', SQLERRM;
END;
$$;