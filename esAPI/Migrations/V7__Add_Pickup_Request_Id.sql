-- Add pickup_request_id column to pickup_requests table
-- This stores the ID that Bulk Logistics gives us when we arrange a pickup

ALTER TABLE pickup_requests 
ADD COLUMN pickup_request_id INTEGER;

COMMENT ON COLUMN pickup_requests.pickup_request_id IS 'The pickup request ID returned by Bulk Logistics API';