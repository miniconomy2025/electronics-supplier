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
    COUNT(e.electronic_id) AS "availableStock",
    lv.electronics_price_per_unit AS "pricePerUnit"
FROM electronics e
INNER JOIN electronics_statuses es ON e.electronics_status = es.status_id
CROSS JOIN (
    SELECT electronics_price_per_unit
    FROM lookup_values
    ORDER BY changed_at DESC
    LIMIT 1
) lv
WHERE es.status = 'AVAILABLE';

CREATE OR REPLACE VIEW machine_status_counts AS
SELECT
    SUM(CASE WHEN ms.status = 'STANDBY' THEN 1 ELSE 0 END) AS "standby",
    SUM(CASE WHEN ms.status = 'IN_USE' THEN 1 ELSE 0 END) AS "inUse",
    SUM(CASE WHEN ms.status = 'BROKEN' THEN 1 ELSE 0 END) AS "broken"
FROM machines m
JOIN machine_statuses ms ON m.machine_status