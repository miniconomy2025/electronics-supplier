CREATE OR REPLACE PROCEDURE add_electronics(amount INT)
LANGUAGE plpgsql
AS $$
DECLARE
    i INT;
    available_status INT;
BEGIN
    -- Get the status_id for 'AVAILABLE'
    SELECT status_id INTO available_status FROM electronics_statuses WHERE status = 'AVAILABLE' LIMIT 1;
    IF available_status IS NULL THEN
        RAISE EXCEPTION 'No AVAILABLE status found in electronics_statuses';
    END IF;

    FOR i IN 1..amount LOOP
        INSERT INTO electronics (produced_at, electronics_status)
        VALUES (EXTRACT(EPOCH FROM NOW()), available_status);
    END LOOP;
END;
$$;
