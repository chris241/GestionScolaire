import { apiClient } from './client';
import type { Room } from '../types';

export async function fetchRooms(): Promise<Room[]> {
  const { data } = await apiClient.get<Room[]>('/rooms');
  return data;
}

export async function createRoom(request: { name: string; capacity: number; building: string | null }): Promise<Room> {
  const { data } = await apiClient.post<Room>('/rooms', request);
  return data;
}

export async function deleteRoom(id: string): Promise<void> {
  await apiClient.delete(`/rooms/${id}`);
}
