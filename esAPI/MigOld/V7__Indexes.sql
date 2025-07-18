-- Indexes for foreign key columns
CREATE INDEX idx_material_supplies_material_id ON material_supplies(material_id);
CREATE INDEX idx_machine_ratios_material_id ON machine_ratios(material_id);
CREATE INDEX idx_machine_ratios_machine_id ON machine_ratios(machine_id);
CREATE INDEX idx_machine_orders_supplier_id ON machine_orders(supplier_id);
CREATE INDEX idx_material_orders_supplier_id ON material_orders(supplier_id);
CREATE INDEX idx_electronics_orders_manufacturer_id ON electronics_orders(manufacturer_id);

-- Indexes for columns used in views/filters
CREATE INDEX idx_material_supplies_processed_at ON material_supplies(processed_at);
CREATE INDEX idx_machines_status ON machines(machine_status);
CREATE INDEX idx_electronics_sold_at ON electronics(sold_at);

-- Unique indexes
CREATE UNIQUE INDEX IF NOT EXISTS idx_materials_material_name ON materials(material_name);