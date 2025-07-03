CREATE OR REPLACE FUNCTION get_all_materials()
RETURNS SETOF materials AS $$
    SELECT * FROM materials;
$$ LANGUAGE sql;

CREATE OR REPLACE FUNCTION get_material_by_id(p_material_id INT)
RETURNS materials AS $$
    SELECT * FROM materials WHERE material_id = p_material_id;
$$ LANGUAGE sql;

CREATE OR REPLACE FUNCTION get_all_supplies()
RETURNS SETOF supplies AS $$
    SELECT * FROM supplies;
$$ LANGUAGE sql;

CREATE OR REPLACE FUNCTION get_supply_by_id(p_supply_id INT)
RETURNS supplies AS $$
    SELECT * FROM supplies WHERE supply_id = p_supply_id;
$$ LANGUAGE sql;

CREATE OR REPLACE FUNCTION get_all_machines()
RETURNS TABLE (
    machine_id INT,
    ratio JSON,
    status VARCHAR(8),
    purchase_price FLOAT,
    purchased_at TIMESTAMPTZ,
    sold_at TIMESTAMPTZ
) AS $$
    SELECT
        m.machine_id,
        COALESCE(json_object_agg(mat.material_name, r.ratio) FILTER (WHERE r.ratio_id IS NOT NULL), '{}') AS ratio,
        ms.status AS status,
        m.purchase_price,
        m.purchased_at,
        NULL::TIMESTAMPTZ AS sold_at
    FROM machines m
    LEFT JOIN machine_ratios r ON m.machine_id = r.machine_id
    LEFT JOIN materials mat ON r.material_id = mat.material_id
    INNER JOIN machine_statuses ms ON m.status_id = ms.status_id
    GROUP BY m.machine_id, ms.status, m.purchase_price, m.purchased_at;
$$ LANGUAGE sql;

CREATE OR REPLACE FUNCTION get_machine_by_id(p_machine_id INT)
RETURNS TABLE (
    machine_id INT,
    ratio JSON,
    status VARCHAR(8),
    purchase_price FLOAT,
    purchased_at TIMESTAMPTZ,
    sold_at TIMESTAMPTZ
) AS $$
    SELECT
        m.machine_id,
        COALESCE(json_object_agg(mat.material_name, r.ratio) FILTER (WHERE r.ratio_id IS NOT NULL), '{}') AS ratio,
        ms.status AS status,
        m.purchase_price,
        m.purchased_at,
        NULL::TIMESTAMPTZ AS sold_at
    FROM machines m
    LEFT JOIN machine_ratios r ON m.machine_id = r.machine_id
    LEFT JOIN materials mat ON r.material_id = mat.material_id
    INNER JOIN machine_statuses ms ON m.status_id = ms.status_id
    WHERE m.machine_id = p_machine_id
    GROUP BY m.machine_id, ms.status, m.purchase_price, m.purchased_at;
$$ LANGUAGE sql;

CREATE OR REPLACE FUNCTION get_all_machine_ratios()
RETURNS SETOF machine_ratios AS $$
    SELECT * FROM machine_ratios;
$$ LANGUAGE sql;

CREATE OR REPLACE FUNCTION get_machine_ratio_by_id(p_ratio_id INT)
RETURNS machine_ratios AS $$
    SELECT * FROM machine_ratios WHERE ratio_id = p_ratio_id;
$$ LANGUAGE sql;

CREATE OR REPLACE FUNCTION get_all_machine_orders()
RETURNS TABLE (
    order_id INT,
    supplier_id INT,
    supplier_name VARCHAR(16),
    ordered_at TIMESTAMPTZ,
    received_at TIMESTAMPTZ,
    status TEXT
) AS $$
    SELECT mo.order_id, mo.supplier_id, ms.supplier_name, mo.ordered_at, mo.received_at,
        CASE WHEN mo.received_at IS NULL THEN 'PENDING' ELSE 'COMPLETED' END AS status
    FROM machine_orders mo
    INNER JOIN material_suppliers ms ON mo.supplier_id = ms.supplier_id;
$$ LANGUAGE sql;

CREATE OR REPLACE FUNCTION get_machine_order_by_id(p_order_id INT)
RETURNS TABLE (
    order_id INT,
    supplier_id INT,
    supplier_name VARCHAR(16),
    ordered_at TIMESTAMPTZ,
    received_at TIMESTAMPTZ,
    status TEXT
) AS $$
    SELECT mo.order_id, mo.supplier_id, ms.supplier_name, mo.ordered_at, mo.received_at,
        CASE WHEN mo.received_at IS NULL THEN 'PENDING' ELSE 'COMPLETED' END AS status
    FROM machine_orders mo
    INNER JOIN material_suppliers ms ON mo.supplier_id = ms.supplier_id
    WHERE mo.order_id = p_order_id;
$$ LANGUAGE sql;

CREATE OR REPLACE FUNCTION get_all_material_suppliers()
RETURNS SETOF material_suppliers AS $$
    SELECT * FROM material_suppliers;
$$ LANGUAGE sql;

CREATE OR REPLACE FUNCTION get_material_supplier_by_id(p_supplier_id INT)
RETURNS material_suppliers AS $$
    SELECT * FROM material_suppliers WHERE supplier_id = p_supplier_id;
$$ LANGUAGE sql;

CREATE OR REPLACE FUNCTION get_all_material_orders()
RETURNS TABLE (
    order_id INT,
    supplier_id INT,
    supplier_name VARCHAR(16),
    ordered_at TIMESTAMPTZ,
    received_at TIMESTAMPTZ,
    status TEXT
) AS $$
    SELECT mo.order_id, mo.supplier_id, ms.supplier_name, mo.ordered_at, mo.received_at,
        CASE WHEN mo.received_at IS NULL THEN 'PENDING' ELSE 'COMPLETED' END AS status
    FROM material_orders mo
    INNER JOIN material_suppliers ms ON mo.supplier_id = ms.supplier_id;
$$ LANGUAGE sql;

CREATE OR REPLACE FUNCTION get_material_order_by_id(p_order_id INT)
RETURNS TABLE (
    order_id INT,
    supplier_id INT,
    supplier_name VARCHAR(16),
    ordered_at TIMESTAMPTZ,
    received_at TIMESTAMPTZ,
    status TEXT
) AS $$
    SELECT mo.order_id, mo.supplier_id, ms.supplier_name, mo.ordered_at, mo.received_at,
        CASE WHEN mo.received_at IS NULL THEN 'PENDING' ELSE 'COMPLETED' END AS status
    FROM material_orders mo
    INNER JOIN material_suppliers ms ON mo.supplier_id = ms.supplier_id
    WHERE mo.order_id = p_order_id;
$$ LANGUAGE sql;

CREATE OR REPLACE FUNCTION get_all_material_order_items()
RETURNS SETOF material_order_items AS $$
    SELECT * FROM material_order_items;
$$ LANGUAGE sql;

CREATE OR REPLACE FUNCTION get_material_order_item_by_id(p_item_id INT)
RETURNS material_order_items AS $$
    SELECT * FROM material_order_items WHERE item_id = p_item_id;
$$ LANGUAGE sql;

CREATE OR REPLACE FUNCTION get_all_phone_manufacturers()
RETURNS SETOF phone_manufacturers AS $$
    SELECT * FROM phone_manufacturers;
$$ LANGUAGE sql;

CREATE OR REPLACE FUNCTION get_phone_manufacturer_by_id(p_manufacturer_id INT)
RETURNS phone_manufacturers AS $$
    SELECT * FROM phone_manufacturers WHERE manufacturer_id = p_manufacturer_id;
$$ LANGUAGE sql;

CREATE OR REPLACE FUNCTION get_all_electronics()
RETURNS SETOF electronics AS $$
    SELECT * FROM electronics;
$$ LANGUAGE sql;

CREATE OR REPLACE FUNCTION get_electronic_by_id(p_electronic_id INT)
RETURNS electronics AS $$
    SELECT * FROM electronics WHERE electronic_id = p_electronic_id;
$$ LANGUAGE sql;

CREATE OR REPLACE FUNCTION get_all_electronics_orders()
RETURNS TABLE (
    order_id INT,
    manufacturer_id INT,
    manufacturer_name VARCHAR(8),
    amount INT,
    ordered_at TIMESTAMPTZ,
    processed_at TIMESTAMPTZ,
    status TEXT
) AS $$
    SELECT eo.order_id, eo.manufacturer_id, pm.manufacturer_name, eo.amount, eo.ordered_at, eo.processed_at,
        CASE WHEN eo.processed_at IS NULL THEN 'PENDING' ELSE 'PROCESSED' END AS status
    FROM electronics_orders eo
    INNER JOIN phone_manufacturers pm ON eo.manufacturer_id = pm.manufacturer_id;
$$ LANGUAGE sql;

CREATE OR REPLACE FUNCTION get_electronics_order_by_id(p_order_id INT)
RETURNS TABLE (
    order_id INT,
    manufacturer_id INT,
    manufacturer_name VARCHAR(8),
    amount INT,
    ordered_at TIMESTAMPTZ,
    processed_at TIMESTAMPTZ,
    status TEXT
) AS $$
    SELECT eo.order_id, eo.manufacturer_id, pm.manufacturer_name, eo.amount, eo.ordered_at, eo.processed_at,
        CASE WHEN eo.processed_at IS NULL THEN 'PENDING' ELSE 'PROCESSED' END AS status
    FROM electronics_orders eo
    INNER JOIN phone_manufacturers pm ON eo.manufacturer_id = pm.manufacturer_id
    WHERE eo.order_id = p_order_id;
$$ LANGUAGE sql;

CREATE OR REPLACE FUNCTION get_all_lookup_values()
RETURNS SETOF lookup_values AS $$
    SELECT * FROM lookup_values;
$$ LANGUAGE sql;

CREATE OR REPLACE FUNCTION get_lookup_value_by_id(p_value_id INT)
RETURNS lookup_values AS $$
    SELECT * FROM lookup_values WHERE value_id = p_value_id;
$$ LANGUAGE sql;