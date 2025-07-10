CREATE OR REPLACE VIEW current_supplies AS
SELECT
    m.material_id AS "materialId",
    m.material_name AS "materialName",
    COUNT(s.supply_id) AS "availableSupply"
FROM materials m
LEFT JOIN material_supplies s ON m.material_id = s.material_id AND s.processed_at IS NULL
GROUP BY m.material_id, m.material_name;

CREATE OR REPLACE VIEW available_electronics_stock AS
SELECT
    (SELECT COUNT(e.electronic_id)
     FROM electronics e
     INNER JOIN electronics_statuses es ON e.electronics_status = es.status_id
     WHERE es.status = 'AVAILABLE') AS "availableStock",
    lv.electronics_price_per_unit AS "pricePerUnit"
FROM (
    SELECT electronics_price_per_unit
    FROM lookup_values
    ORDER BY changed_at DESC
    LIMIT 1
) lv;

CREATE OR REPLACE VIEW machine_status_counts AS
SELECT
    SUM(CASE WHEN ms.status = 'STANDBY' THEN 1 ELSE 0 END) AS "standby",
    SUM(CASE WHEN ms.status = 'IN_USE' THEN 1 ELSE 0 END) AS "inUse",
    SUM(CASE WHEN ms.status = 'BROKEN' THEN 1 ELSE 0 END) AS "broken"
FROM machines m
INNER JOIN machine_statuses ms ON m.machine_status = ms.status_id;

CREATE OR REPLACE VIEW effective_material_stock AS
WITH
 
  physical_stock AS (
    SELECT
      ms.material_id,
      COUNT(ms.supply_id) AS quantity
    FROM material_supplies ms
    GROUP BY ms.material_id
  ),
  
  pending_stock AS (
    SELECT
      mo.material_id,
      SUM(mo.remaining_amount) AS quantity
    FROM material_orders mo
    JOIN order_statuses os ON mo.order_status = os.status_id
    WHERE os.status NOT IN ('COMPLETED', 'DISASTER', 'REJECTED', 'EXPIRED')
    GROUP BY mo.material_id
  )

SELECT
  m.material_id,
  m.material_name,
  (COALESCE(ps.quantity, 0) + COALESCE(os.quantity, 0)) AS effective_quantity
FROM materials m
LEFT JOIN physical_stock ps ON m.material_id = ps.material_id
LEFT JOIN pending_stock os ON m.material_id = os.material_id;
