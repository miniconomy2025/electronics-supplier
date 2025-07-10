CREATE OR REPLACE FUNCTION get_inventory_summary()
RETURNS jsonb AS $$
DECLARE
    summary jsonb;
BEGIN
    SELECT jsonb_build_object(
        'machines', (
            SELECT jsonb_build_object(
                'total', COUNT(*),
                'standby', COUNT(*) FILTER (WHERE ms.status = 'STANDBY'),
                'inUse', COUNT(*) FILTER (WHERE ms.status = 'IN_USE'),
                'broken', COUNT(*) FILTER (WHERE ms.status = 'BROKEN')
            )
            FROM machines m
            JOIN machine_statuses ms ON m.machine_status = ms.status_id
        ),
        'materialsInStock', (
            SELECT jsonb_agg(
                jsonb_build_object(
                    'materialId', cs."materialId",
                    'materialName', cs."materialName",
                    'quantity', cs."availableSupply"
                )
            )
            FROM current_supplies cs
        ),
        'electronicsInStock', (
            SELECT COUNT(*) FROM electronics WHERE sold_at IS NULL
        )
    ) INTO summary;

    RETURN summary;
END;
$$ LANGUAGE plpgsql;