-- Change pickup_requests.type from enum to varchar to avoid Entity Framework mapping issues

-- First, add a new varchar column
ALTER TABLE pickup_requests 
ADD COLUMN type_temp VARCHAR(20);

-- Copy existing data, converting enum values to strings
UPDATE pickup_requests 
SET type_temp = type::text;

-- Drop the old enum column
ALTER TABLE pickup_requests 
DROP COLUMN type;

-- Rename the temp column to the original name
ALTER TABLE pickup_requests 
RENAME COLUMN type_temp TO type;

-- Add NOT NULL constraint
ALTER TABLE pickup_requests 
ALTER COLUMN type SET NOT NULL;

-- Add a check constraint to ensure valid values
ALTER TABLE pickup_requests 
ADD CONSTRAINT pickup_requests_type_check 
CHECK (type IN ('MACHINE', 'COPPER', 'SILICON'));

-- Drop the enum type since we're no longer using it
DROP TYPE IF EXISTS request_type;