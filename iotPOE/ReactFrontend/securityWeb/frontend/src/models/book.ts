// we use the model to determine what the data coming from and going to the API will look like

export interface Book {
  _id?: string;
  title: string;
  author: string;
  isbn: string;
  publishedYear?: number;
  genre: "Fiction" | "Non-Fiction" | "Textbook";
  createdAt?: string;
  updatedAt?: string;
}
/** 
References
Rudman, G., 2026. insy7314-library. (Version 1.0) [Source Code] Available at: < https://github.com/rudderz243/insy7314-library > [Accessed 29 August 2026]
*/