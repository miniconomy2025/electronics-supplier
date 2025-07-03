CREATE OR REPLACE PROCEDURE update_material(
    IN p_material_id INT,
    IN p_material_name VARCHAR(8) DEFAULT NULL
) LANGUAGE plpgsql AS $$
BEGIN

    IF p_material_id IS NULL OR NOT EXISTS (SELECT 1 FROM materials WHERE material_id = p_material_id) THEN
        RAISE EXCEPTION 'Material ID % does not exist', p_material_id;
    END IF;

    IF p_material_name IS NOT NULL AND LENGTH(TRIM(p_material_name)) = 0 THEN
        RAISE EXCEPTION 'Material name must not be empty if provided';
    END IF;

    UPDATE materials
    SET material_name = COALESCE(p_material_name, material_name)
    WHERE material_id = p_material_id;

    RAISE NOTICE 'Material updated successfully';

EXCEPTION
    WHEN unique_violation THEN
        RAISE EXCEPTION 'Material name must be unique';
    WHEN others THEN
        RAISE EXCEPTION 'Error updating material: %', SQLERRM;

END;
$$;

CREATE OR REPLACE PROCEDURE update_supply(
    IN p_supply_id INT,
    IN p_material_id INT DEFAULT NULL,
    IN p_received_at TIMESTAMPTZ DEFAULT NULL,
    IN p_processed_at TIMESTAMPTZ DEFAULT NULL
) LANGUAGE plpgsql AS $$
BEGIN

    IF p_supply_id IS NULL OR NOT EXISTS (SELECT 1 FROM supplies WHERE supply_id = p_supply_id) THEN
        RAISE EXCEPTION 'Supply ID % does not exist', p_supply_id;
    END IF;

    IF p_material_id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM materials WHERE material_id = p_material_id) THEN
        RAISE EXCEPTION 'Material ID % does not exist', p_material_id;
    END IF;

    IF p_received_at IS NOT NULL AND p_received_at > NOW() THEN
        RAISE EXCEPTION 'Received at must be in the past';
    END IF;

    UPDATE supplies
    SET material_id = COALESCE(p_material_id, material_id),
        received_at = COALESCE(p_received_at, received_at),
        processed_at = COALESCE(p_processed_at, processed_at)
    WHERE supply_id = p_supply_id;

    RAISE NOTICE 'Supply updated successfully';

EXCEPTION
    WHEN others THEN
        RAISE EXCEPTION 'Error updating supply: %', SQLERRM;

END;
$$;

CREATE OR REPLACE PROCEDURE update_machine(
    IN p_machine_id INT,
    IN p_status_id INT DEFAULT NULL,
    IN p_purchase_price FLOAT DEFAULT NULL,
    IN p_purchased_at TIMESTAMPTZ DEFAULT NULL
) LANGUAGE plpgsql AS $$
BEGIN

    IF p_machine_id IS NULL OR NOT EXISTS (SELECT 1 FROM machines WHERE machine_id = p_machine_id) THEN
        RAISE EXCEPTION 'Machine ID % does not exist', p_machine_id;
    END IF;

    IF p_status_id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM machine_statuses WHERE status_id = p_status_id) THEN
        RAISE EXCEPTION 'Status ID % does not exist', p_status_id;
    END IF;

    IF p_purchase_price IS NOT NULL AND p_purchase_price <= 0 THEN
        RAISE EXCEPTION 'Purchase price must be greater than 0';
    END IF;

    IF p_purchased_at IS NOT NULL AND p_purchased_at > NOW() THEN
        RAISE EXCEPTION 'Purchased at must be in the past';
    END IF;

    UPDATE machines
    SET status_id = COALESCE(p_status_id, status_id),
        purchase_price = COALESCE(p_purchase_price, purchase_price),
        purchased_at = COALESCE(p_purchased_at, purchased_at)
    WHERE machine_id = p_machine_id;

    RAISE NOTICE 'Machine updated successfully';

EXCEPTION
    WHEN others THEN
        RAISE EXCEPTION 'Error updating machine: %', SQLERRM;

END;
$$;

CREATE OR REPLACE PROCEDURE update_machine_ratio(
    IN p_ratio_id INT,
    IN p_material_id INT DEFAULT NULL,
    IN p_ratio INT DEFAULT NULL,
    IN p_machine_id INT DEFAULT NULL
) LANGUAGE plpgsql AS $$
BEGIN

    IF p_ratio_id IS NULL OR NOT EXISTS (SELECT 1 FROM machine_ratios WHERE ratio_id = p_ratio_id) THEN
        RAISE EXCEPTION 'Ratio ID % does not exist', p_ratio_id;
    END IF;

    IF p_material_id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM materials WHERE material_id = p_material_id) THEN
        RAISE EXCEPTION 'Material ID % does not exist', p_material_id;
    END IF;

    IF p_machine_id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM machines WHERE machine_id = p_machine_id) THEN
        RAISE EXCEPTION 'Machine ID % does not exist', p_machine_id;
    END IF;

    IF p_ratio IS NOT NULL AND p_ratio <= 0 THEN
        RAISE EXCEPTION 'Ratio must be a positive integer';
    END IF;

    UPDATE machine_ratios
    SET material_id = COALESCE(p_material_id, material_id),
        ratio = COALESCE(p_ratio, ratio),
        machine_id = COALESCE(p_machine_id, machine_id)
    WHERE ratio_id = p_ratio_id;

    RAISE NOTICE 'Machine ratio updated successfully';

EXCEPTION
    WHEN others THEN
        RAISE EXCEPTION 'Error updating machine ratio: %', SQLERRM;

END;
$$;

CREATE OR REPLACE PROCEDURE update_machine_order(
    IN p_order_id INT,
    IN p_supplier_id INT DEFAULT NULL,
    IN p_ordered_at TIMESTAMPTZ DEFAULT NULL,
    IN p_received_at TIMESTAMPTZ DEFAULT NULL
) LANGUAGE plpgsql AS $$
BEGIN

    IF p_order_id IS NULL OR NOT EXISTS (SELECT 1 FROM machine_orders WHERE order_id = p_order_id) THEN
        RAISE EXCEPTION 'Order ID % does not exist', p_order_id;
    END IF;

    IF p_supplier_id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM material_suppliers WHERE supplier_id = p_supplier_id) THEN
        RAISE EXCEPTION 'Supplier ID % does not exist', p_supplier_id;
    END IF;

    IF p_ordered_at IS NOT NULL AND p_ordered_at > NOW() THEN
        RAISE EXCEPTION 'Ordered at must be a valid past timestamp';
    END IF;

    IF p_received_at IS NOT NULL AND p_ordered_at IS NOT NULL AND p_received_at < p_ordered_at THEN
        RAISE EXCEPTION 'Received at cannot be before ordered at';
    END IF;

    UPDATE machine_orders
    SET supplier_id = COALESCE(p_supplier_id, supplier_id),
        ordered_at = COALESCE(p_ordered_at, ordered_at),
        received_at = COALESCE(p_received_at, received_at)
    WHERE order_id = p_order_id;

    RAISE NOTICE 'Machine order updated successfully';

EXCEPTION
    WHEN others THEN
        RAISE EXCEPTION 'Error updating machine order: %', SQLERRM;

END;
$$;

CREATE OR REPLACE PROCEDURE update_material_supplier(
    IN p_supplier_id INT,
    IN p_supplier_name VARCHAR(16) DEFAULT NULL
) LANGUAGE plpgsql AS $$
BEGIN

    IF p_supplier_id IS NULL OR NOT EXISTS (SELECT 1 FROM material_suppliers WHERE supplier_id = p_supplier_id) THEN
        RAISE EXCEPTION 'Supplier ID % does not exist', p_supplier_id;
    END IF;

    IF p_supplier_name IS NOT NULL AND LENGTH(TRIM(p_supplier_name)) = 0 THEN
        RAISE EXCEPTION 'Supplier name must not be empty if provided';
    END IF;

    UPDATE material_suppliers
    SET supplier_name = COALESCE(p_supplier_name, supplier_name)
    WHERE supplier_id = p_supplier_id;

    RAISE NOTICE 'Material supplier updated successfully';

EXCEPTION
    WHEN others THEN
        RAISE EXCEPTION 'Error updating material supplier: %', SQLERRM;

END;
$$;

CREATE OR REPLACE PROCEDURE update_material_order(
    IN p_order_id INT,
    IN p_supplier_id INT DEFAULT NULL,
    IN p_ordered_at TIMESTAMPTZ DEFAULT NULL,
    IN p_received_at TIMESTAMPTZ DEFAULT NULL
) LANGUAGE plpgsql AS $$
BEGIN

    IF p_order_id IS NULL OR NOT EXISTS (SELECT 1 FROM material_orders WHERE order_id = p_order_id) THEN
        RAISE EXCEPTION 'Order ID % does not exist', p_order_id;
    END IF;

    IF p_supplier_id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM material_suppliers WHERE supplier_id = p_supplier_id) THEN
        RAISE EXCEPTION 'Supplier ID % does not exist', p_supplier_id;
    END IF;

    IF p_ordered_at IS NOT NULL AND p_ordered_at > NOW() THEN
        RAISE EXCEPTION 'Ordered at must be a valid past timestamp';
    END IF;

    IF p_received_at IS NOT NULL AND p_ordered_at IS NOT NULL AND p_received_at < p_ordered_at THEN
        RAISE EXCEPTION 'Received at cannot be before ordered at';
    END IF;

    UPDATE material_orders
    SET supplier_id = COALESCE(p_supplier_id, supplier_id),
        ordered_at = COALESCE(p_ordered_at, ordered_at),
        received_at = COALESCE(p_received_at, received_at)
    WHERE order_id = p_order_id;

    RAISE NOTICE 'Material order updated successfully';

EXCEPTION
    WHEN others THEN
        RAISE EXCEPTION 'Error updating material order: %', SQLERRM;

END;
$$;

CREATE OR REPLACE PROCEDURE update_material_order_item(
    IN p_item_id INT,
    IN p_material_id INT DEFAULT NULL,
    IN p_amount INT DEFAULT NULL,
    IN p_order_id INT DEFAULT NULL
) LANGUAGE plpgsql AS $$
BEGIN

    IF p_item_id IS NULL OR NOT EXISTS (SELECT 1 FROM material_order_items WHERE item_id = p_item_id) THEN
        RAISE EXCEPTION 'Item ID % does not exist', p_item_id;
    END IF;

    IF p_material_id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM materials WHERE material_id = p_material_id) THEN
        RAISE EXCEPTION 'Material ID % does not exist', p_material_id;
    END IF;

    IF p_order_id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM material_orders WHERE order_id = p_order_id) THEN
        RAISE EXCEPTION 'Order ID % does not exist', p_order_id;
    END IF;

    IF p_amount IS NOT NULL AND p_amount <= 0 THEN
        RAISE EXCEPTION 'Amount must be a positive integer';
    END IF;

    UPDATE material_order_items
    SET material_id = COALESCE(p_material_id, material_id),
        amount = COALESCE(p_amount, amount),
        order_id = COALESCE(p_order_id, order_id)
    WHERE item_id = p_item_id;

    RAISE NOTICE 'Material order item updated successfully';

EXCEPTION
    WHEN others THEN
        RAISE EXCEPTION 'Error updating material order item: %', SQLERRM;

END;
$$;

CREATE OR REPLACE PROCEDURE update_phone_manufacturer(
    IN p_manufacturer_id INT,
    IN p_manufacturer_name VARCHAR(8) DEFAULT NULL
) LANGUAGE plpgsql AS $$
BEGIN

    IF p_manufacturer_id IS NULL OR NOT EXISTS (SELECT 1 FROM phone_manufacturers WHERE manufacturer_id = p_manufacturer_id) THEN
        RAISE EXCEPTION 'Manufacturer ID % does not exist', p_manufacturer_id;
    END IF;

    IF p_manufacturer_name IS NOT NULL AND LENGTH(TRIM(p_manufacturer_name)) = 0 THEN
        RAISE EXCEPTION 'Manufacturer name must not be empty if provided';
    END IF;

    UPDATE phone_manufacturers
    SET manufacturer_name = COALESCE(p_manufacturer_name, manufacturer_name)
    WHERE manufacturer_id = p_manufacturer_id;

    RAISE NOTICE 'Phone manufacturer updated successfully';

EXCEPTION
    WHEN others THEN
        RAISE EXCEPTION 'Error updating phone manufacturer: %', SQLERRM;

END;
$$;

CREATE OR REPLACE PROCEDURE update_electronic(
    IN p_electronic_id INT,
    IN p_produced_at TIMESTAMPTZ DEFAULT NULL,
    IN p_sold_at TIMESTAMPTZ DEFAULT NULL
) LANGUAGE plpgsql AS $$
BEGIN

    IF p_electronic_id IS NULL OR NOT EXISTS (SELECT 1 FROM electronics WHERE electronic_id = p_electronic_id) THEN
        RAISE EXCEPTION 'Electronic ID % does not exist', p_electronic_id;
    END IF;

    IF p_produced_at IS NOT NULL AND p_produced_at > NOW() THEN
        RAISE EXCEPTION 'Produced at must be a valid past timestamp';
    END IF;

    IF p_sold_at IS NOT NULL AND p_produced_at IS NOT NULL AND p_sold_at < p_produced_at THEN
        RAISE EXCEPTION 'Sold at cannot be before produced at';
    END IF;

    UPDATE electronics
    SET produced_at = COALESCE(p_produced_at, produced_at),
        sold_at = COALESCE(p_sold_at, sold_at)
    WHERE electronic_id = p_electronic_id;

    RAISE NOTICE 'Electronic updated successfully';

EXCEPTION
    WHEN others THEN
        RAISE EXCEPTION 'Error updating electronic: %', SQLERRM;

END;
$$;

CREATE OR REPLACE PROCEDURE update_electronics_order(
    IN p_order_id INT,
    IN p_manufacturer_id INT DEFAULT NULL,
    IN p_amount INT DEFAULT NULL,
    IN p_ordered_at TIMESTAMPTZ DEFAULT NULL,
    IN p_processed_at TIMESTAMPTZ DEFAULT NULL
) LANGUAGE plpgsql AS $$
BEGIN

    IF p_order_id IS NULL OR NOT EXISTS (SELECT 1 FROM electronics_orders WHERE order_id = p_order_id) THEN
        RAISE EXCEPTION 'Order ID % does not exist', p_order_id;
    END IF;

    IF p_manufacturer_id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM phone_manufacturers WHERE manufacturer_id = p_manufacturer_id) THEN
        RAISE EXCEPTION 'Manufacturer ID % does not exist', p_manufacturer_id;
    END IF;

    IF p_amount IS NOT NULL AND p_amount <= 0 THEN
        RAISE EXCEPTION 'Amount must be a positive integer';
    END IF;

    IF p_ordered_at IS NOT NULL AND p_ordered_at > NOW() THEN
        RAISE EXCEPTION 'Ordered at must be a valid past timestamp';
    END IF;

    IF p_processed_at IS NOT NULL AND p_ordered_at IS NOT NULL AND p_processed_at < p_ordered_at THEN
        RAISE EXCEPTION 'Processed at cannot be before ordered at';
    END IF;

    UPDATE electronics_orders
    SET manufacturer_id = COALESCE(p_manufacturer_id, manufacturer_id),
        amount = COALESCE(p_amount, amount),
        ordered_at = COALESCE(p_ordered_at, ordered_at),
        processed_at = COALESCE(p_processed_at, processed_at)
    WHERE order_id = p_order_id;

    RAISE NOTICE 'Electronics order updated successfully';

EXCEPTION
    WHEN others THEN
        RAISE EXCEPTION 'Error updating electronics order: %', SQLERRM;

END;
$$;

CREATE OR REPLACE PROCEDURE update_lookup_value(
    IN p_value_id INT,
    IN p_electronics_price FLOAT DEFAULT NULL
) LANGUAGE plpgsql AS $$
BEGIN

    IF p_value_id IS NULL OR NOT EXISTS (SELECT 1 FROM lookup_values WHERE value_id = p_value_id) THEN
        RAISE EXCEPTION 'Value ID % does not exist', p_value_id;
    END IF;

    IF p_electronics_price IS NOT NULL AND p_electronics_price <= 0 THEN
        RAISE EXCEPTION 'Electronics price must be a positive number';
    END IF;

    UPDATE lookup_values
    SET electronics_price = COALESCE(p_electronics_price, electronics_price)
    WHERE value_id = p_value_id;

    RAISE NOTICE 'Lookup value updated successfully';

EXCEPTION
    WHEN others THEN
        RAISE EXCEPTION 'Error updating lookup value: %', SQLERRM;

END;
$$;