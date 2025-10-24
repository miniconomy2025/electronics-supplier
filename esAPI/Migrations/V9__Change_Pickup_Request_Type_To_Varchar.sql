-- Change pickup_requests.type to align with Bulk Logistics API (PICKUP/DELIVERY)

-- Add a new varchar column for operation type
ALTER TABLE pickup_requests 
ADD COLUMN type_temp VARCHAR(20);

-- Set all existing records to 'PICKUP' since we only do pickups
UPDATE pickup_requests 
SET type_temp = 'PICKUP';

-- Drop the old enum column
ALTER TABLE pickup_requests 
DROP COLUMN type;

-- Rename the temp column to type
ALTER TABLE pickup_requests 
RENAME COLUMN type_temp TO type;

-- Add NOT NULL constraint
ALTER TABLE pickup_requests 
ALTER COLUMN type SET NOT NULL;

-- Add check constraint for Bulk Logistics API operation types
ALTER TABLE pickup_requests 
ADD CONSTRAINT pickup_requests_type_check 
CHECK (type IN ('PICKUP', 'DELIVERY'));

-- Drop the enum type since we're no longer using it
DROP TYPE IF EXISTS request_type;