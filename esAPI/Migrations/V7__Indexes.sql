-- Indexes for foreign key columns
CREATE INDEX idx_supplies_material_id ON supplies(material_id);
CREATE INDEX idx_machine_ratios_material_id ON machine_ratios(material_id);
CREATE INDEX idx_machine_ratios_machine_id ON machine_ratios(machine_id);
CREATE INDEX idx_machine_orders_supplier_id ON machine_orders(supplier_id);
CREATE INDEX idx_material_orders_supplier_id ON material_orders(supplier_id);
CREATE INDEX idx_material_order_items_material_id ON material_order_items(material_id);
CREATE INDEX idx_material_order_items_order_id ON material_order_items(order_id);
CREATE INDEX idx_electronics_orders_manufacturer_id ON electronics_orders(manufacturer_id);

-- Indexes for columns used in views/filters
CREATE INDEX idx_supplies_processed_at ON supplies(processed_at);
CREATE INDEX idx_machines_status ON machines(status_id);
CREATE INDEX idx_machines_status_id ON machines(status_id);
CREATE INDEX idx_electronics_sold_at ON electronics(sold_at);

-- Unique indexes
CREATE UNIQUE INDEX IF NOT EXISTS idx_materials_material_name ON materials(material_name);
CREATE UNIQUE INDEX IF NOT EXISTS idx_material_suppliers_supplier_name ON material_suppliers(supplier_name);
CREATE UNIQUE INDEX IF NOT EXISTS idx_phone_manufacturers_manufacturer_name ON phone_manufacturers(manufacturer_name);