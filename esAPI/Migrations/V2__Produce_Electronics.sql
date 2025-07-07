CREATE OR REPLACE FUNCTION produce_electronics()
RETURNS TABLE (
    electronics_created INT,
    copper_used INT,
    silicone_used INT
)
LANGUAGE plpgsql
AS $$
DECLARE
    current_day INT;
    copper_needed INT;
    silicone_needed INT;
    available_copper INT;
    available_silicone INT;
    units_by_copper INT;
    units_by_silicone INT;
    units_possible INT;
    total_machine_output INT := 0;
    total_units INT := 0;
    i INT;
    in_use_status INT;
    standby_status INT;
    machine_rec RECORD;
    machines_used INT := 0;
    machine_ids INT[] := ARRAY[]::INT[];
    units_remaining INT;
BEGIN
    -- 1. Get current simulation day
    SELECT day_number INTO current_day FROM simulation ORDER BY simulation_id DESC LIMIT 1;

    -- 2. Get required ratios (assuming only copper and silicone for simplicity)
    SELECT ratio INTO copper_needed FROM machine_ratios WHERE material_id = (SELECT material_id FROM materials WHERE material_name = 'copper');
    SELECT ratio INTO silicone_needed FROM machine_ratios WHERE material_id = (SELECT material_id FROM materials WHERE material_name = 'silicone');

    -- 3. Get available (unprocessed) materials
    SELECT COALESCE(SUM(1), 0) INTO available_copper FROM material_supplies ms
        JOIN materials m ON ms.material_id = m.material_id
        WHERE m.material_name = 'copper' AND ms.processed_at IS NULL;
    SELECT COALESCE(SUM(1), 0) INTO available_silicone FROM material_supplies ms
        JOIN materials m ON ms.material_id = m.material_id
        WHERE m.material_name = 'silicone' AND ms.processed_at IS NULL;

    -- 4. Calculate how many units can be produced from each material
    units_by_copper := FLOOR(available_copper / copper_needed);
    units_by_silicone := FLOOR(available_silicone / silicone_needed);

    -- 5. Get the status IDs for IN_USE and STANDBY
    SELECT status_id INTO in_use_status FROM machine_statuses WHERE status = 'IN_USE';
    SELECT status_id INTO standby_status FROM machine_statuses WHERE status = 'STANDBY';

    -- 6. Select STANDBY machines ordered by machine_id, accumulate their outputs until we reach the units_possible
    units_possible := LEAST(units_by_copper, units_by_silicone);
    units_remaining := units_possible;
    FOR machine_rec IN 
        SELECT m.machine_id, md.maximum_output
        FROM machines m
        JOIN machine_statuses ms ON m.machine_status = ms.status_id
        JOIN machine_details md ON md.detail_id = m.machine_id
        WHERE ms.status = 'STANDBY'
        ORDER BY m.machine_id
    LOOP
        IF units_remaining <= 0 THEN
            EXIT;
        END IF;
        machine_ids := array_append(machine_ids, machine_rec.machine_id);
        IF machine_rec.maximum_output <= units_remaining THEN
            total_machine_output := total_machine_output + machine_rec.maximum_output;
            units_remaining := units_remaining - machine_rec.maximum_output;
        ELSE
            total_machine_output := total_machine_output + units_remaining;
            units_remaining := 0;
        END IF;
    END LOOP;
    total_units := total_machine_output;
    IF total_units = 0 THEN
        RETURN;
    END IF;

    -- Set selected machines to IN_USE
    UPDATE machines
    SET machine_status = in_use_status
    WHERE machine_id = ANY(machine_ids);

    -- Produce electronics
    FOR i IN 1..total_units LOOP
        INSERT INTO electronics (produced_at, electronics_status)
        VALUES (current_day, 1); -- 1 = AVAILABLE
    END LOOP;

    -- Mark only the used copper supplies as processed
    WITH copper_to_update AS (
        SELECT ms.supply_id
        FROM material_supplies ms
        JOIN materials m ON ms.material_id = m.material_id
        WHERE m.material_name = 'copper' AND ms.processed_at IS NULL
        ORDER BY ms.supply_id
        LIMIT copper_needed * total_units
    )
    UPDATE material_supplies
    SET processed_at = current_day
    WHERE supply_id IN (SELECT supply_id FROM copper_to_update);

    -- Mark only the used silicone supplies as processed
    WITH silicone_to_update AS (
        SELECT ms.supply_id
        FROM material_supplies ms
        JOIN materials m ON ms.material_id = m.material_id
        WHERE m.material_name = 'silicone' AND ms.processed_at IS NULL
        ORDER BY ms.supply_id
        LIMIT silicone_needed * total_units
    )
    UPDATE material_supplies
    SET processed_at = current_day
    WHERE supply_id IN (SELECT supply_id FROM silicone_to_update);

    -- Set machines back to STANDBY
    UPDATE machines
    SET machine_status = standby_status
    WHERE machine_id = ANY(machine_ids);

    -- Return the number of electronics created and materials used
    RETURN QUERY
    SELECT total_units AS electronics_created,
           copper_needed * total_units AS copper_used,
           silicone_needed * total_units AS silicone_used;

END
$$