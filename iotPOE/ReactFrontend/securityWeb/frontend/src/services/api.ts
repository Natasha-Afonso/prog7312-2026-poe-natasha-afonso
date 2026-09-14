import type { Book } from "../models/book.ts";
import type { SensorReading } from "../models/telemetry";
import type { AlertDto } from "../models/alert.ts";

import type { Sensor } from "../models/sensor.ts"; // Imports sensor interface

const API_URL = "http://localhost:8080/api"; 


// SENSOR API SERVICES

// GET ALL SENSORS
export async function getSensors(): Promise<Sensor[]> {
  const response = await fetch(`${API_URL}/Sensors`);
  if (!response.ok) throw new Error("Failed to fetch sensors");
  return response.json();
}

// GET SINGLE SENSOR BY ID
export async function getSensor(id: number): Promise<Sensor> {
  const response = await fetch(`${API_URL}/Sensors/${id}`);
  if (!response.ok) throw new Error("Failed to fetch sensor details");
  return response.json();
}

// CREATE NEW SENSOR 
export async function createSensor(formData: FormData): Promise<Sensor> {
  const response = await fetch(`${API_URL}/Sensors`, {
    method: "POST",
    body: formData, // Browser automatically sets boundary header
  });

  if (!response.ok) {
    const errorText = await response.text();
    throw new Error(errorText || "Failed to register sensor");
  }

  return response.json();
}

// DELETE SENSOR
export async function deleteSensor(id: number): Promise<Sensor> {
  const response = await fetch(`${API_URL}/Sensors/${id}`, {
    method: "DELETE",
  });
  if (!response.ok) throw new Error("Failed to delete sensor");
  return response.json();
}

export const getTelemetryBySensorId = async (
  sensorId: string | number
): Promise<SensorReading[]> => {
  const response = await fetch(`${API_URL}/Telemetry/${sensorId}`);
  if (!response.ok) {
    throw new Error(`Failed to fetch telemetry for sensor ID: ${sensorId}`);
  }
  return response.json();
};

// ALERTS API SERVICES

export async function getAlerts(): Promise<AlertDto[]> {
  const response = await fetch(`${API_URL}/Alerts`);
  if (!response.ok) throw new Error("Failed to fetch alerts");
  return response.json();
}

export async function acknowledgeAlert(id: number): Promise<void> {
  const getResponse = await fetch(`${API_URL}/Alerts/${id}`);
  if (!getResponse.ok) throw new Error("Failed to fetch alert before acknowledging");
  const alert = await getResponse.json();

  const putResponse = await fetch(`${API_URL}/Alerts/${id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ ...alert, status: "acknowledged" }),
  });

  if (!putResponse.ok) throw new Error("Failed to acknowledge alert");
}
// this function will get ALL books from the API
export async function getBooks(): Promise<Book[]> {
  // fetch(http://localhost:3000/api/books)
  const response = await fetch(`${API_URL}/books`);
  return response.json();
}

// this function will get a SINGLE book based on ID
export async function getBook(id: string): Promise<Book> {
  // fetch(http://localhost:3000/api/books/abc123)
  const response = await fetch(`${API_URL}/books/${id}`);
  return response.json();
}

// this function will CREATE a new book using a POST request
export async function createBook(book: Book): Promise<Book> {
  const response = await fetch(`${API_URL}/books`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(book),
  });
  return response.json();
}

// this function will update sections of an existing Book in the database using PATCH
export async function updateBook(book: Book, id: string): Promise<Book> {
  const response = await fetch(`${API_URL}/books/${id}`, {
    method: "PATCH",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(book),
  });
  return response.json();
}

// this function will replace an entire book in the database using PUT
export async function replaceBook(book: Book, id: string): Promise<Book> {
  const response = await fetch(`${API_URL}/books/${id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(book),
  });
  return response.json();
}

// this function will delete a book from the database using a DELETE request
export async function deleteBook(id: string): Promise<Book> {
  const response = await fetch(`${API_URL}/books/${id}`, {
    method: "DELETE",
  });
  return response.json();
}
