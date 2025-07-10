CREATE OR REPLACE VIEW daily_material_consumption AS
WITH
  -- Get all operational machines
  operational_machines AS (
    SELECT m.machine_id, m.order_id
    FROM machines m
    JOIN machine_statuses ms ON m.machine_status = ms.status_id
    WHERE ms.status IN ('STANDBY', 'IN_USE')
  ),
  all_defined_ratios AS (
    SELECT
        mr.material_id,
        mr.ratio
    FROM machine_ratios mr
    JOIN machine_details md ON mr.ratio_id = md.ratio_id
  )
SELECT
  ar.material_id,
  mat.material_name,
  -- Total consumption = sum of ratios for all machine types * number of operational machines.
  SUM(ar.ratio) * (SELECT COUNT(*) FROM operational_machines) AS total_daily_consumption
FROM all_defined_ratios ar
JOIN materials mat ON ar.material_id = mat.material_id
GROUP BY ar.material_id, mat.material_name;